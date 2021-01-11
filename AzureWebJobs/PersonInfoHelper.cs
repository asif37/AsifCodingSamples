using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TexellCheckInWebJob.Models.Edmx;

namespace TexellCheckInWebJob.Helpers
{
    public static class PersonInfoHelper
    {
        public static string DecryptFile(string inputFileName, string keyFileName, string publicKeyFilename,
             char[] passwd)
        {
            string decrypted = string.Empty;
            using (Stream input = File.OpenRead(inputFileName),
                   keyIn = File.OpenRead(keyFileName),
                   publicKey = File.OpenRead(publicKeyFilename))
            {
                decrypted = DecryptFile(inputFileName, input, keyIn, publicKey, passwd);
            }
            return decrypted;
        }

        public static string DecryptFile(string fileName, Stream inputStream, Stream keyIn, Stream publicKeyStream,
            char[] passwd)
        {
            string decryptedContent = "";

            inputStream = PgpUtilities.GetDecoderStream(inputStream);

            try
            {
                inputStream = PgpUtilities.GetDecoderStream(inputStream);

                PgpObjectFactory pgpF = new PgpObjectFactory(inputStream);
                PgpEncryptedDataList enc;

                Object o = pgpF.NextPgpObject();

                if (o is PgpEncryptedDataList)
                {
                    enc = (PgpEncryptedDataList)o;
                }
                else
                {
                    enc = (PgpEncryptedDataList)pgpF.NextPgpObject();
                }

                //find the secret key
                PgpPrivateKey sKey = null;
                PgpPublicKeyEncryptedData pbe = null;
                PgpSecretKeyRingBundle pgpSec = new PgpSecretKeyRingBundle(
                    PgpUtilities.GetDecoderStream(keyIn));

                foreach (PgpPublicKeyEncryptedData pked in enc.GetEncryptedDataObjects())
                {
                    sKey = FindSecretKey(pgpSec, pked.KeyId, passwd);

                    if (sKey != null)
                    {
                        pbe = pked;
                        break;
                    }
                }

                if (sKey == null)
                {
                    throw new ArgumentException("secret key for message not found.");
                }

                Stream clear = pbe.GetDataStream(sKey);

                PgpObjectFactory plainFact = new PgpObjectFactory(clear);

                Object message;

                PgpSignatureList signatureList = null;
                PgpCompressedData compressedData;

                message = plainFact.NextPgpObject();

                while (message != null)
                {
                    if (message is PgpCompressedData)
                    {
                        compressedData = (PgpCompressedData)message;
                        plainFact = new PgpObjectFactory(compressedData.GetDataStream());
                        message = plainFact.NextPgpObject();
                    }

                    if (message is PgpLiteralData)
                    {
                        Stream decryptStream = ((PgpLiteralData)message).GetInputStream();
                        using (MemoryStream memStream = new MemoryStream())
                        {
                            decryptStream.CopyTo(memStream);
                            byte[] decrypted = memStream.ToArray();
                            decryptedContent = Encoding.ASCII.GetString(decrypted);
                        }
                    }
                    else if (message is PgpSignatureList)
                    {
                        signatureList = (PgpSignatureList)message;
                    }
                    else
                    {
                        throw new PgpException("message unknown type.");
                    }

                    message = plainFact.NextPgpObject();
                }

                PgpPublicKeyRingBundle pgpPubRingCollection = new PgpPublicKeyRingBundle(PgpUtilities.GetDecoderStream(publicKeyStream));
                Stream dIn = File.OpenRead(fileName);
                PgpSignature sig = signatureList[0];
                PgpPublicKey key = pgpPubRingCollection.GetPublicKey(sig.KeyId);
                sig.InitVerify(key);

                int ch;
                while ((ch = dIn.ReadByte()) >= 0)
                {
                    sig.Update((byte)ch);
                }

                dIn.Close();

                if (sig.Verify())
                {
                    //signature verified
                }
                else
                {
                    //signature not found
                }

                return decryptedContent;
            }
            catch (PgpException e)
            {
                throw e;
            }
        }

        public static PgpPrivateKey FindSecretKey(PgpSecretKeyRingBundle pgpSec, long keyID, char[] pass)
        {
            PgpSecretKey pgpSecKey = pgpSec.GetSecretKey(keyID);

            if (pgpSecKey == null)
            {
                return null;
            }

            return pgpSecKey.ExtractPrivateKey(pass);
        }

        public static bool UpdatePersonUsingImportFile(string content)
        {
            try
            {
                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    DateTime today = TimeZoneHelper.ConvertToServerTimeZone(DateTime.Now);

                    var result = content.Split(new[] { '\r', '\n' }).Where(r => r != null && r != "").ToArray();
                    string account = "", enagement = "";
                    if (result.Length > 0)
                    {
                        int length = result.Length;
                        //the first line (index = 0) is the header: Account, etc.
                        Console.WriteLine("[{0}][PERSON INFO FILE] Total members found in Person Info Import File: {1}.",
                            today,
                            (length - 1).ToString());

                        //Loop through all records in Person Info File. Split the row into Account and Engagement and identify if a person
                        //record already exists, if true then update person record otherwise create new person record
                        for (int k = 1; k < length; k++)
                        {
                            if (result[k] != "" && result[k].Length > 0)
                            {
                                var personFields = result[k].Split(',');
                                if (personFields.Length == 2)
                                {
                                    account = personFields[0].Trim();
                                    enagement = personFields[1].Trim();

                                    //check if the person already exists in the database
                                    Person existingPerson = db.People.SqlQuery("SELECT * FROM [dbo].[Person] WHERE [AccountNumber] IS NOT NULL AND [AccountNumber] = @p0", account)
                                        .DefaultIfEmpty(null).FirstOrDefault();
                                    if (existingPerson != null)
                                    {
                                        db.Database.ExecuteSqlCommand("UPDATE [dbo].[Person] SET [Engagement] = @p0, [VerificationDate] = @p1 WHERE [Id] = @p2",
                                        enagement, today, existingPerson.Id);

                                        Console.WriteLine("[{0}][PERSON INFO FILE] Member {1}/{2} ({3}) updated successfully.",
                                               today,
                                               k.ToString(),
                                               (length - 1).ToString(),
                                               account);
                                    }
                                    else
                                    {
                                        db.Database.ExecuteSqlCommand("INSERT INTO [dbo].[Person] ([Engagement], [AccountNumber], [VerificationDate], [IsNonMember]) VALUES(" +
                                            "@p0, @p1, @p2, @p3)", enagement, account, today, null);

                                        Console.WriteLine("[{0}][PERSON INFO FILE] Member {1}/{2} - ({3}) added to database successfully.",
                                            today,
                                            k.ToString(),
                                            (length - 1).ToString(),
                                            account);
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[{0}] Processing Person Info Import File failed. Exception: {1}.", TimeZoneHelper.GetServerTimeStamp(), ex.Message));
                ExceptionHelper.Throw(ex, nameof(UpdatePersonUsingImportFile));
                return false;
            }
        }

        public static bool UpdatePersonNameUsingImportFile(string content)
        {
            try
            {
                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    DateTime today = TimeZoneHelper.ConvertToServerTimeZone(DateTime.Now);

                    var result = content.Split(new[] { '\r', '\n' }).Where(r => r != null && r != "").ToArray();
                    string nameId = "", firstName = "", lastName = "";
                    long accountNo;
                    if (result.Length > 0)
                    {
                        int length = result.Length;
                        //the first line (index = 0) is the header: Account, etc.
                        Console.WriteLine("[{0}][PERSON NAME INFO FILE] Total person names found in person name info file: {1}.",
                            today,
                            (length - 1).ToString());

                        //Loop through all records in Person Name import file. 
                        for (int k = 1; k < length; k++)
                        {
                            if (result[k] != "" && result[k].Length > 0)
                            {
                                var personFields = result[k].Split(',');
                                if (personFields.Length == 4)
                                {
                                    accountNo = long.Parse(personFields[0].Trim());
                                    nameId = personFields[1].Trim();
                                    firstName = personFields[2].Trim();
                                    lastName = personFields[3].Trim();

                                    //check if the personName already exists in the database
                                    PersonName existingPerson = db.PersonNames.SqlQuery("SELECT * FROM [dbo].[PersonName] WHERE [NameId] IS NOT NULL AND [NameId] = @p0", nameId).DefaultIfEmpty(null).FirstOrDefault();
                                    if (existingPerson != null)
                                    {
                                        db.Database.ExecuteSqlCommand("UPDATE [dbo].[PersonName] SET [FirstName] = @p0, [LastName] = @p1, [VerificationDate] = @p2 WHERE [NameId] = @p3",
                                        firstName, lastName, today, existingPerson.NameId);

                                        Console.WriteLine("[{0}][MEMBER NAME INFO FILE] Member {1}/{2} ({3}) {4} {5} updated successfully.",
                                               today,
                                               k.ToString(),
                                               (length - 1).ToString(),
                                               nameId,
                                               firstName,
                                               lastName);
                                    }
                                    else
                                    {
                                        //if we are creating a new record we first must identify the personId associated with this name
                                        var personId = db.People.First(r => r.AccountNumber == accountNo).Id;

                                        if (personId != 0)
                                        {
                                            db.Database.ExecuteSqlCommand("INSERT INTO [dbo].[PersonName] ([FirstName], [LastName], [VerificationDate], [NameId], [IdPerson]) VALUES(" +
                                                "@p0, @p1, @p2, @p3, @p4)", firstName, lastName, today, nameId, personId);

                                            Console.WriteLine("[{0}][MEMBER NAME INFO FILE] Member {1}/{2} - ({3}) {4} {5} added to database successfully.",
                                                today,
                                                k.ToString(),
                                                (length - 1).ToString(),
                                                nameId,
                                                firstName,
                                                lastName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[{0}] Processing member name info file failed. Exception: {1}.", TimeZoneHelper.GetServerTimeStamp(), ex.Message));
                ExceptionHelper.Throw(ex, nameof(UpdatePersonNameUsingImportFile));
                return false;
            }
        }

        public static bool UpdatePersonCardUsingImportFile(string content)
        {
            try
            {

                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    DateTime today = TimeZoneHelper.ConvertToServerTimeZone(DateTime.Now);

                    var result = content.Split(new[] { '\r', '\n' }).Where(r => r != null && r != "").ToArray();
                    int account = 0;
                    long card = 0;
                    if (result.Length > 0)
                    {
                        int length = result.Length;
                        //the first line (index = 0) is the header: Account, etc.
                        Console.WriteLine("[{0}][PERSON CARD INFO FILE] Total person cards found in Person Card Info Import File: {1}.",
                            today,
                            (length - 1).ToString());

                        //Loop through all records in Person Info File. Split the row into Account and Card Number and identify if a PersonCard
                        //record already exists, if true then update PersonCard otherwise create new PersonCard record
                        for (int k = 1; k < length; k++)
                        {
                            if (result[k] != "" && result[k].Length > 0)
                            {
                                var cardFields = result[k].Split(',');
                                if (cardFields.Length == 2)
                                {
                                    account = int.Parse(cardFields[0].Trim());
                                    card = long.Parse(cardFields[1].Trim());

                                    //check if the card already exists in the database
                                    PersonCard existingCard = db.PersonCards.FirstOrDefault(c => c.AccountNumber == account && c.CardNumber == card);

                                    //If the card already exists, update its verification date; otherwise add the new card if a matching account is found
                                    if (existingCard != null)
                                    {
                                        existingCard.VerificationDate = today;
                                        db.SaveChanges();

                                        Console.WriteLine("[{0}][PERSON CARD INFO FILE] Card {1}/{2} ({3}) updated successfully.",
                                               today,
                                               k.ToString(),
                                               (length - 1).ToString(),
                                               account);
                                    }
                                    else
                                    {
                                        PersonCard newCard = new PersonCard()
                                        {
                                            AccountNumber = account,
                                            CardNumber = card,
                                            VerificationDate = today
                                        };

                                        Person person = db.People.FirstOrDefault(p => p.AccountNumber == account);
                                        if (person != null)
                                        {
                                            newCard.IdPerson = person.Id;
                                            db.PersonCards.Add(newCard);
                                            db.SaveChanges();

                                            Console.WriteLine("[{0}][PERSON CARD INFO FILE] Card {1}/{2} - ({3}) added to database successfully.",
                                            today,
                                            k.ToString(),
                                            (length - 1).ToString(),
                                            account);
                                        }
                                        else
                                        {
                                            // The person does not exist but the card does; likely a closed or otherwise inaccessible account but not necessarily an error
                                            Console.WriteLine("[{0}][PERSON CARD INFO FILE] Card {1}/{2} - ({3}) did not match an existing member.",
                                            today,
                                            k.ToString(),
                                            (length - 1).ToString(),
                                            account);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[{0}] Processing member name info file failed. Exception: {1}.", TimeZoneHelper.GetServerTimeStamp(), ex.Message));
                ExceptionHelper.Throw(ex, nameof(UpdatePersonCardUsingImportFile));
                return false;
            }
        }
    }
}
