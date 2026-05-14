using System;
using System.Runtime.InteropServices;

#if UNITY_STANDALONE_WIN
public static class NativeFilePicker
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int flagsEx;
    }

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetOpenFileName(ref OpenFileName ofn);

    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_NOCHANGEDIR = 0x00000008;

    public static string PickImageFile(string title = "选择图片")
    {
        var ofn = new OpenFileName();
        ofn.lStructSize = Marshal.SizeOf(ofn);
        ofn.lpstrFilter = "图片文件\0*.png;*.jpg;*.jpeg;*.bmp\0所有文件\0*.*\0\0";
        ofn.nFilterIndex = 1;
        ofn.lpstrFile = new string('\0', 512);
        ofn.nMaxFile = 512;
        ofn.lpstrTitle = title;
        ofn.flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

        return GetOpenFileName(ref ofn) ? ofn.lpstrFile.TrimEnd('\0') : null;
    }

    public static string PickMediaFile(string title = "选择媒体文件")
    {
        var ofn = new OpenFileName();
        ofn.lStructSize = Marshal.SizeOf(ofn);
        ofn.lpstrFilter = "媒体文件\0*.png;*.jpg;*.jpeg;*.bmp;*.mp4;*.webm;*.avi;*.mov;*.mp3;*.ogg;*.wav\0图片\0*.png;*.jpg;*.jpeg;*.bmp\0视频\0*.mp4;*.webm;*.avi;*.mov\0音频\0*.mp3;*.ogg;*.wav\0所有文件\0*.*\0\0";
        ofn.nFilterIndex = 1;
        ofn.lpstrFile = new string('\0', 512);
        ofn.nMaxFile = 512;
        ofn.lpstrTitle = title;
        ofn.flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

        return GetOpenFileName(ref ofn) ? ofn.lpstrFile.TrimEnd('\0') : null;
    }
}
#else
public static class NativeFilePicker
{
    public static string PickImageFile(string title = "选择图片") => null;
    public static string PickMediaFile(string title = "选择媒体文件") => null;
}
#endif
