using System.Buffers.Binary;

namespace Scope.Core;

public static class UsbTmcPacket

{

	public static byte[] Write(byte tag, byte[] data)

	{

		byte[] packet=Header(1, tag, data.Length, 12+(data.Length+3)/4*4);
		packet[8]=1;
		data.CopyTo(packet, 12);
		return packet;

	}

	public static byte[] Request(byte tag, int length) => Header(2, tag, length, 12);
	private static byte[] Header(byte id, byte tag, int length, int size)

	{

		if (tag == 0 || length < 0 || length > Scpi.MaxMessage)
			throw new InvalidDataException("Nieprawidłowy pakiet USBTMC.");
		byte[] b=new byte[size];
		b[0]=id;
		b[1]=tag;
		b[2]=(byte)~tag;
		BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(4), length);
		return b;

	}

	public static (byte[] Data, bool End) Read(byte tag, byte[] packet)

	{

		if (packet.Length < 12 || packet[0] != 2 || packet[1] != tag || packet[2] != (byte)~tag)
			throw new InvalidDataException("Nieprawidłowy nagłówek odpowiedzi USBTMC.");
		int length=BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(4));
		if (length < 0 || length > packet.Length-12)
			throw new InvalidDataException("Niepełna odpowiedź USBTMC.");
		return (packet.AsSpan(12, length).ToArray(), (packet[8]&1) != 0);

	}


}
