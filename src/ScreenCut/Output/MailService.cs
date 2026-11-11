using System.Runtime.InteropServices;
using System.Text;
using ScreenCut.Core;

namespace ScreenCut.Output;

/// <summary>
/// Opens a new e-mail with the screenshot attached. Uses Simple MAPI (classic Outlook, Thunderbird…);
/// when no MAPI client is available (e.g. new Outlook) it falls back to an unsent .eml draft.
/// </summary>
internal static class MailService
{
    private const string Subject = "Ekran görüntüsü";
    private const int MapiSuccess = 0;
    private const int MapiUserAbort = 1;
    private const uint MapiLogonUi = 0x00000001;
    private const uint MapiDialog = 0x00000008;
    private const int Base64LineLength = 76;

    public static void Compose(string attachmentPath, Action<Exception> onError)
    {
        // MAPISendMail is modal and must run on an STA thread; never block the UI thread with it.
        var thread = new Thread(() =>
        {
            try
            {
                var result = SendWithMapi(attachmentPath);
                if (result is not MapiSuccess and not MapiUserAbort)
                {
                    Log.Info($"MAPI kullanılamadı (kod {result}); .eml taslağı açılıyor.");
                    OpenEmlDraft(attachmentPath);
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or IOException)
            {
                try
                {
                    OpenEmlDraft(attachmentPath);
                }
                catch (Exception fallbackError)
                {
                    onError(fallbackError);
                }
            }
            catch (Exception ex)
            {
                onError(ex);
            }
        })
        {
            IsBackground = true,
            Name = "ScreenCut.Mail",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    private static int SendWithMapi(string attachmentPath)
    {
        var fileDescriptor = new MapiFileDesc
        {
            Position = uint.MaxValue,
            PathName = attachmentPath,
            FileName = Path.GetFileName(attachmentPath),
        };
        var filePointer = Marshal.AllocHGlobal(Marshal.SizeOf<MapiFileDesc>());
        try
        {
            Marshal.StructureToPtr(fileDescriptor, filePointer, fDeleteOld: false);
            var message = new MapiMessage
            {
                Subject = Subject,
                NoteText = string.Empty,
                FileCount = 1,
                Files = filePointer,
            };
            return MAPISendMailW(IntPtr.Zero, IntPtr.Zero, message, MapiLogonUi | MapiDialog, 0);
        }
        finally
        {
            Marshal.DestroyStructure<MapiFileDesc>(filePointer);
            Marshal.FreeHGlobal(filePointer);
        }
    }

    private static void OpenEmlDraft(string attachmentPath)
    {
        var boundary = "----=_ScreenCut_" + Guid.NewGuid().ToString("N");
        var fileName = Path.GetFileName(attachmentPath);
        var encodedSubject = "=?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(Subject)) + "?=";
        var attachment = Convert.ToBase64String(File.ReadAllBytes(attachmentPath));

        var builder = new StringBuilder()
            .Append("X-Unsent: 1\r\n")
            .Append("Subject: ").Append(encodedSubject).Append("\r\n")
            .Append("MIME-Version: 1.0\r\n")
            .Append("Content-Type: multipart/mixed; boundary=\"").Append(boundary).Append("\"\r\n\r\n")
            .Append("--").Append(boundary).Append("\r\n")
            .Append("Content-Type: text/plain; charset=utf-8\r\n\r\n\r\n")
            .Append("--").Append(boundary).Append("\r\n")
            .Append("Content-Type: image/png; name=\"").Append(fileName).Append("\"\r\n")
            .Append("Content-Transfer-Encoding: base64\r\n")
            .Append("Content-Disposition: attachment; filename=\"").Append(fileName).Append("\"\r\n\r\n");

        for (var i = 0; i < attachment.Length; i += Base64LineLength)
        {
            builder.Append(attachment, i, Math.Min(Base64LineLength, attachment.Length - i)).Append("\r\n");
        }

        builder.Append("--").Append(boundary).Append("--\r\n");

        var emlPath = Path.ChangeExtension(attachmentPath, ".eml");
        File.WriteAllText(emlPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        AppLauncher.OpenShell(emlPath);
    }

    [DllImport("mapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "MAPISendMailW")]
    private static extern int MAPISendMailW(IntPtr session, IntPtr uiParam, MapiMessage message, uint flags, uint reserved);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class MapiMessage
    {
        public uint Reserved;
        public string? Subject;
        public string? NoteText;
        public string? MessageType;
        public string? DateReceived;
        public string? ConversationId;
        public uint Flags;
        public IntPtr Originator;
        public uint RecipientCount;
        public IntPtr Recipients;
        public uint FileCount;
        public IntPtr Files;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MapiFileDesc
    {
        public uint Reserved;
        public uint Flags;
        public uint Position;
        public string? PathName;
        public string? FileName;
        public IntPtr FileType;
    }
}
