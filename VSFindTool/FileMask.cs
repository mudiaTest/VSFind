using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Controls;


namespace VSFindTool
{
    class FileMaskItem{
        public string Value { get; set; }
        public string Key { get; set; }
        public override string ToString() { return this.Value; }
    }

    class FileMask
    {
        static private string VsRegKey = "SOFTWARE\\VsFindTool\\FileMasks";
        static private RegistryKey GetFileMasksKey()
        {
            RegistryKey myKey = Registry.CurrentUser.OpenSubKey(VsRegKey, true);
            if (myKey == null)
            {
                Registry.CurrentUser.CreateSubKey(VsRegKey);
                myKey = Registry.CurrentUser.OpenSubKey(VsRegKey, true);
            }
            return myKey;
        }

        static public void AddToRegistry(string mask)
        {
            if (mask == "" || mask == "*.cs")
                return;
            RegistryKey myKey = GetFileMasksKey();
            if (myKey != null)
            {
                 myKey.SetValue("key" + myKey.ValueCount, mask, RegistryValueKind.String);
                myKey.Close();
            }
        }

        static public void DelFromRegistry(string name)
        {
            if (name == "")
                return;
            RegistryKey myKey = GetFileMasksKey();
            myKey.DeleteValue(name);            
        }

        static public void FillCB(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new FileMaskItem() { Key = "", Value = "*.cs"});
            RegistryKey myKey = GetFileMasksKey();
            foreach (string key in myKey.GetValueNames())
            {
                cb.Items.Add(new FileMaskItem() { Key = key, Value = (string)myKey.GetValue(key) });
            }
        }
    }
}
