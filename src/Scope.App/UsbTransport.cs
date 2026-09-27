using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Scope.Core;

namespace Scope.App;

public sealed record UsbDevice(string Path, string Name)

{

	public override string ToString() => Name;

}


public sealed class UsbTransport : IInstrumentTransport

{

	private readonly SafeFileHandle file;
	private IntPtr usb;
	private byte input, output, tag;
	private bool disposed;
	private const int PayloadSize=65536;

	public static List<UsbDevice> Enumerate()

	{

		HashSet<Guid> guids=[new("A5DCBF10-6530-11D2-901F-00C04FB951ED")];
		using RegistryKey? devices=Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
		if (devices != null)

		{

			foreach (string id in devices.GetSubKeyNames().Where(n => n.StartsWith("VID_F4EC", StringComparison.OrdinalIgnoreCase)))

			{

				using RegistryKey? model=devices.OpenSubKey(id);
				if (model == null)
					continue;
				foreach (string serial in model.GetSubKeyNames())

				{

					using RegistryKey? parameters=model.OpenSubKey(serial+@"\Device Parameters");
					object? value=parameters?.GetValue("DeviceInterfaceGUIDs");
					string[] values=value is string[] array ? array : value is string text ? [text] : [];
					foreach (string candidate in values)
						if (Guid.TryParse(candidate, out Guid parsed))
							guids.Add(parsed);

				}


			}


		}

		Dictionary<string, UsbDevice> found=new(StringComparer.OrdinalIgnoreCase);
		foreach (Guid candidate in guids)

		{

			Guid guid=candidate;
			IntPtr set=Native.SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, 18);
			if (set == new IntPtr(-1))
				continue;
			try

			{

				for (uint index=0; ; index++)

				{

					Native.DeviceInterface info=new()

					{

						Size=Marshal.SizeOf<Native.DeviceInterface>()

					};
					if (!Native.SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, index, ref info))
						break;
					Native.SetupDiGetDeviceInterfaceDetail(set, ref info, IntPtr.Zero, 0, out uint required, IntPtr.Zero);
					if (required < 8 || required > 65536)
						continue;
					IntPtr detail=Marshal.AllocHGlobal((int)required);
					try

					{

						Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
						if (!Native.SetupDiGetDeviceInterfaceDetail(set, ref info, detail, required, out _, IntPtr.Zero))
							continue;
						string path=Marshal.PtrToStringUni(detail+4) ?? "";
						if (!path.Contains("vid_f4ec", StringComparison.OrdinalIgnoreCase))
							continue;
						string[] parts=path.Split('#');
						string name="SIGLENT "+(parts.Length > 2 ? parts[2] : "");
						found[path]=new(path, name);

					}

					finally
					{
						Marshal.FreeHGlobal(detail);
					}


				}


			}

			finally
			{
				Native.SetupDiDestroyDeviceInfoList(set);
			}


		}

		return found.Values.ToList();

	}

	public UsbTransport(string path)

	{

		file=Native.CreateFile(path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
		if (file.IsInvalid)

		{

			file.Dispose();
			throw Error("Nie można otworzyć USB");

		}

		try

		{

			if (!Native.WinUsb_Initialize(file, out usb))
				throw Error("Urządzenie wymaga sterownika WinUSB. W oscyloskopie ustaw Back USB = USBTMC. Instrukcja znajduje się w README");
			if (!Native.WinUsb_QueryInterfaceSettings(usb, 0, out Native.InterfaceDescriptor descriptor))
				throw Error("Odczyt interfejsu USB");
			if (descriptor.Class != 0xFE || descriptor.SubClass != 3)
				throw new IOException("Wybrany interfejs nie jest USBTMC.");
			for (byte i=0; i < descriptor.Endpoints; i++)

			{

				if (!Native.WinUsb_QueryPipe(usb, 0, i, out Native.Pipe pipe))
					throw Error("Odczyt endpointów");
				if (pipe.Type != 2)
					continue;
				if ((pipe.Id&0x80) != 0)
					input=pipe.Id;
				else
					output=pipe.Id;

			}

			if (input == 0 || output == 0)
				throw new IOException("Brak pary endpointów bulk USBTMC.");
			uint timeout=10000;
			if (!Native.WinUsb_SetPipePolicy(usb, input, 3, 4, ref timeout) || !Native.WinUsb_SetPipePolicy(usb, output, 3, 4, ref timeout))
				throw Error("Konfiguracja czasu oczekiwania USB");

		}

		catch
		{
			Dispose();
			throw;
		}


	}

	private static IOException Error(string operation) => new(operation+": "+new Win32Exception(Marshal.GetLastWin32Error()).Message);
	private byte NextTag()

	{

		tag=tag == 255 ? (byte)1 : (byte)(tag+1);
		return tag;

	}

	private void Send(byte[] bytes)

	{

		if (!Native.WinUsb_WritePipe(usb, output, bytes, (uint)bytes.Length, out uint sent, IntPtr.Zero) || sent != bytes.Length)
			throw Error("Zapis USB");

	}

	public void Write(string command)

	{

		ObjectDisposedException.ThrowIf(disposed, this);
		Send(UsbTmcPacket.Write(NextTag(), Encoding.ASCII.GetBytes(command+"\n")));

	}

	public byte[] Query(string command)

	{

		Write(command);
		using MemoryStream result=new();
		while (true)

		{

			byte current=NextTag();
			Send(UsbTmcPacket.Request(current, PayloadSize));
			using MemoryStream packet=new();
			int expected=12;
			while (packet.Length < expected)

			{

				byte[] part=new byte[PayloadSize+512];
				if (!Native.WinUsb_ReadPipe(usb, input, part, (uint)part.Length, out uint count, IntPtr.Zero))
					throw Error("Odczyt USB");
				if (count == 0)
					throw new IOException("Pusty pakiet USB.");
				packet.Write(part, 0, (int)count);
				if (packet.Length >= 12)

				{

					int length=BinaryPrimitives.ReadInt32LittleEndian(packet.GetBuffer().AsSpan(4));
					if (length < 0 || length > PayloadSize)
						throw new InvalidDataException("Nieprawidłowa długość pakietu USB.");
					expected=12+(length+3)/4*4;

				}


			}

			var response=UsbTmcPacket.Read(current, packet.ToArray());
			if (response.Data.Length == 0 && !response.End)
				throw new IOException("Brak danych USB.");
			if (result.Length+response.Data.Length > Scpi.MaxMessage)
				throw new IOException("Przekroczony limit danych.");
			result.Write(response.Data);
			if (response.End)
				return result.ToArray();

		}


	}

	public void Dispose()

	{

		if (disposed)
			return;
		disposed=true;
		if (usb != IntPtr.Zero)

		{

			Native.WinUsb_Free(usb);
			usb=IntPtr.Zero;

		}

		file.Dispose();

	}


	private static class Native

	{

		[StructLayout(LayoutKind.Sequential)]
		internal struct DeviceInterface

		{

			public int Size; public Guid Guid; public uint Flags; public IntPtr Reserved;

		}

		[StructLayout(LayoutKind.Sequential, Pack=1)]
		internal struct InterfaceDescriptor

		{

			public byte Length, Type, Number, Alternate, Endpoints, Class, SubClass, Protocol, Index;

		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct Pipe

		{

			public int Type; public byte Id; public ushort MaxPacket; public byte Interval;

		}

		[DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)]
		internal static extern IntPtr SetupDiGetClassDevs(ref Guid guid, IntPtr enumerator, IntPtr parent, uint flags);
		[DllImport("setupapi.dll", SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr device, ref Guid guid, uint index, ref DeviceInterface info);
		[DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref DeviceInterface info, IntPtr detail, uint size, out uint required, IntPtr device);
		[DllImport("setupapi.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
		[DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
		internal static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
		[DllImport("winusb.dll", SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool WinUsb_Initialize(SafeFileHandle file, out IntPtr handle);
		[DllImport("winusb.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool WinUsb_Free(IntPtr handle);
		[DllImport("winusb.dll", SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool WinUsb_QueryInterfaceSettings(IntPtr handle, byte alternate, out InterfaceDescriptor descriptor);
		[DllImport("winusb.dll", SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool WinUsb_QueryPipe(IntPtr handle, byte alternate, byte index, out Pipe pipe);
		[DllImport("winusb.dll", SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool WinUsb_SetPipePolicy(IntPtr handle, byte pipe, uint policy, uint length, ref uint value);
		[DllImport("winusb.dll", SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool WinUsb_WritePipe(IntPtr handle, byte pipe, byte[] data, uint length, out uint transferred, IntPtr overlapped);
		[DllImport("winusb.dll", SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool WinUsb_ReadPipe(IntPtr handle, byte pipe, byte[] data, uint length, out uint transferred, IntPtr overlapped);

	}


}
