using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBMFont
{
    class MyPrefs
    {
        static public string sConfigPath { set; get; }

        const string cRoot = @"Software\MyBMFont";

        static public string GetString(string pKey, string pDefaultValue = default(string))
        {
            var tRegistryKey = Registry.CurrentUser.OpenSubKey(cRoot);
            if (tRegistryKey != null)
            {
                var tValue = tRegistryKey.GetValue(pKey);
                tRegistryKey.Close();

                return tValue == null ? pDefaultValue : tValue.ToString();
            }

            return pDefaultValue;
        }

        static public void SetString(string pKey, string pValue)
        {
            var tRegistryKey = Registry.CurrentUser.CreateSubKey(cRoot);
            tRegistryKey.SetValue(pKey, pValue);
            tRegistryKey.Close();
        }
    }
}
