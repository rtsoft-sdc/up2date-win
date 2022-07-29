using System.Runtime.InteropServices;

namespace Up2dateClient
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct KeyValuePair
    {
        public string Key;
        public string Value;

        public KeyValuePair(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
