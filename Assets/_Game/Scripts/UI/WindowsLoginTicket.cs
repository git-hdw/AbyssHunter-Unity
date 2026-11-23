using System;
using System.Runtime.InteropServices;

namespace AbyssHunter.UI
{
    // Store only the opaque PlayFab ticket in the current Windows user's local credential vault.
    internal static class WindowsLoginTicket
    {
        private const uint Generic = 1;
        private const uint LocalMachine = 2;
        private const int NotFound = 1168;
        private const int MaxBytes = 2560;
        private static string Target(string title) => "AbyssHunter/PlayFab/" + title;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public uint Flags, Type;
            public string TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint BlobSize;
            public IntPtr Blob;
            public uint Persist, AttributeCount;
            public IntPtr Attributes, Alias, UserName;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredReadW(string target, uint type, uint flags, out IntPtr credential);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWriteW(ref Credential credential, uint flags);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDeleteW(string target, uint type, uint flags);
        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr pointer);

        public static bool TryRead(string title, out string ticket)
        {
            ticket = null;
            if (!CredReadW(Target(title), Generic, 0, out var handle)) return Marshal.GetLastWin32Error() == NotFound;
            try
            {
                var saved = Marshal.PtrToStructure<Credential>(handle);
                if (saved.BlobSize > 0 && saved.BlobSize <= MaxBytes && saved.BlobSize % 2 == 0)
                {
                    var value = Marshal.PtrToStringUni(saved.Blob, (int)saved.BlobSize / 2);
                    if (Valid(value)) ticket = value;
                }
                return true;
            }
            finally { CredFree(handle); }
        }

        public static bool Write(string title, string ticket)
        {
            if (!Valid(ticket)) return false;
            var blob = Marshal.StringToHGlobalUni(ticket);
            try
            {
                var credential = new Credential { Type = Generic, TargetName = Target(title),
                    Blob = blob, BlobSize = (uint)ticket.Length * 2, Persist = LocalMachine };
                return CredWriteW(ref credential, 0);
            }
            finally { Marshal.ZeroFreeGlobalAllocUnicode(blob); }
        }

        private static bool Valid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxBytes / 2) return false;
            foreach (char c in value) if (c < '!' || c > '~') return false;
            return true;
        }

        public static bool Forget(string title) =>
            CredDeleteW(Target(title), Generic, 0) || Marshal.GetLastWin32Error() == NotFound;
    }
}
