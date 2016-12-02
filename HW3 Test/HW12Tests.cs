using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace CS422
{
	[TestFixture()]
	public class HW12Tests
	{
		[Test()]
		public void TestCaseNoSeek1 () //make sure you can't seek on a No Seek Memory Stream
		{
			byte[] arr = new byte[512];
			byte[] arr2 = new byte[512];

			NoSeekMemoryStream NSMSOne = new NoSeekMemoryStream (arr);
			NoSeekMemoryStream NSMSTwo = new NoSeekMemoryStream (arr2, 6, 12);
			Assert.AreEqual (NSMSOne.CanSeek, false); //make sure both streams cannot seek
			Assert.AreEqual (NSMSTwo.CanSeek, false);

			Assert.Throws<NotImplementedException> (() => NSMSOne.Seek (12, System.IO.SeekOrigin.Begin)); //make sure both of these throw exceptions.
			Assert.Throws<NotImplementedException> (() => NSMSTwo.Seek (12, System.IO.SeekOrigin.Begin));

			long pos1 = NSMSOne.Position; //check the position is working correctly
			long pos2 = NSMSTwo.Position;

			Assert.AreEqual (pos1, 0); //check that positions are correct
			Assert.AreEqual (pos1, 0);

			Assert.Throws<Exception> (() => NSMSOne.Position = 2); //check you cannot set the position (seeking)
			Assert.Throws<Exception> (() => NSMSTwo.Position = 2);
		}

		[Test()]
		public void TestCaseConcatStream1 ()
		{
			//tests with two memory streams that can seek
			MemoryStream MS1 = new MemoryStream ();
			MemoryStream MS2 = new MemoryStream ();
			MemoryStream MS3 = new MemoryStream ();
			MemoryStream MS4 = new MemoryStream ();

			ConcatStream CS = new ConcatStream (MS1, MS2);
			ConcatStream CS2 = new ConcatStream (MS3, MS4, 16);

			long len1 = CS.Length;
			long len2 = CS2.Length;

			Assert.AreEqual (len1, 0); //make sure the lengths are as expected.
			Assert.AreEqual (len2, 16);

			long pos1 = CS.Position;
			long pos2 = CS2.Position;


			CS.Seek (7, SeekOrigin.Begin); //seek to position 7
			CS2.Seek (7, SeekOrigin.Begin);

			long pos1AfterSeek = CS.Position; //check position
			long pos2AfterSeek = CS2.Position;

			Assert.AreEqual (7, pos1AfterSeek);
			Assert.AreEqual (7, pos2AfterSeek);

			string SixteenChars = "1234567890123456"; //check if you write 16 bytes it can read the 16 bytes
			byte[] SixteenBytes = new Byte[16];
			SixteenBytes = Encoding.ASCII.GetBytes (SixteenChars);

			CS.Write (SixteenBytes, 0, 16);
			CS2.Write (SixteenBytes, 0, 16);

			CS.Seek (7, SeekOrigin.Begin);
			CS2.Seek (7, SeekOrigin.Begin);

			byte[] readerBytes = new byte[16];
			byte[] readerBytes2 = new byte[16];
			string tempRead;

			CS.Read (readerBytes, 0, 16);
			tempRead = Encoding.Default.GetString (readerBytes);
			Assert.AreEqual (tempRead, SixteenChars);

			CS2.Read (readerBytes2, 0, 16); //This should not read in 16 bytes... length is only 16 and we're not at 0
			tempRead = "";
			tempRead = Encoding.Default.GetString (readerBytes2);

			Assert.AreNotEqual (tempRead, SixteenBytes); //CS2.Read should have only read in 7 bytes. (add a bunch of nulls to the end of byte array).

			Assert.DoesNotThrow(() => CS.Flush());
			CS.Position = 0;
			Assert.AreEqual (CS.Position, 0);

			CS.SetLength (64); //make sure you cannot set length if length was not set initially
			CS2.SetLength (64); //make sure you can set length

			Assert.AreNotEqual (CS.Length, 64);
			Assert.AreEqual (CS2.Length, 64);


		}

		[Test()]
		public void TestCaseConcatStream2 ()
		{
			byte[] buf = new byte[16];
			byte[] buf2 = new byte[16];
			MemoryStream MS = new MemoryStream (buf);
			MemoryStream MS2 = new MemoryStream (buf2);

			ConcatStream CS = new ConcatStream (MS, MS2);

			Assert.AreEqual (CS.Position, 0);
			Assert.AreEqual (CS.Length, 32); 

			byte[] EighteenBytes = new byte[18];
			EighteenBytes = Encoding.ASCII.GetBytes ("123456789012345678");

			CS.Write (EighteenBytes, 0, 18);

			CS.Seek (0, SeekOrigin.Begin);

			byte[] EighteenReader = new byte[18];
			string temp = "";

			CS.Read (EighteenReader, 0, 18);
			temp = Encoding.Default.GetString (EighteenReader);

			Assert.Throws<Exception> (() => CS.Seek (-6, SeekOrigin.End)); 
		}

		[Test()]
		public void TestCaseConcatStreamWithNoSeekMemStream ()
		{
			byte[] buff = new byte[16];
			byte[] buff2 = new byte[16];

			MemoryStream MS = new MemoryStream (buff);
			NoSeekMemoryStream NSMS = new NoSeekMemoryStream (buff2);

			ConcatStream CS = new ConcatStream (MS, NSMS);

			Assert.AreEqual (CS.CanSeek, false); //because we have one stream that cannot seek, we should assume this concat stream cannnot seek

			Assert.Throws<Exception> (() => CS.Position = 2);
			Assert.Throws<NotSupportedException> (() => CS.Seek(2, SeekOrigin.Begin));

			byte[] EighteenBytes = new byte[18];
			EighteenBytes = Encoding.ASCII.GetBytes ("123456789012345678");

			Assert.DoesNotThrow(() => CS.Write (EighteenBytes, 0, 18)); 
		}
	}
}


