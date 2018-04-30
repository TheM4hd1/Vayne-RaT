using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vayne_Rat
{
    class Compiler
    {
        public static bool CompileFromSource(string source, string Output, string Icon = null, string[] Resources = null)
        {
            CompilerParameters CParams = new CompilerParameters();
            CParams.GenerateExecutable = true;
            CParams.OutputAssembly = Output;
            string options = "/optimize+ /platform:x86 /target:winexe /unsafe";
            if (Icon != null)
                options += " /win32icon:\"" + Icon + "\"";

            CParams.CompilerOptions = options;
            CParams.TreatWarningsAsErrors = false;
            CParams.ReferencedAssemblies.Add("System.dll");
            CParams.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            CParams.ReferencedAssemblies.Add("System.Drawing.dll");
            CParams.ReferencedAssemblies.Add("System.Data.dll");
            CParams.ReferencedAssemblies.Add("Microsoft.VisualBasic.dll");

            if (Resources != null && Resources.Length > 0)
            {
                foreach (string res in Resources)
                {
                    CParams.EmbeddedResources.Add(res);
                }
            }

            Dictionary<string, string> ProviderOptions = new Dictionary<string, string>();
            ProviderOptions.Add("CompilerVersion", "v2.0");

            CompilerResults Results = new CSharpCodeProvider(ProviderOptions).CompileAssemblyFromSource(CParams, source);

            if (Results.Errors.Count > 0)
            {
                MessageBox.Show(string.Format("The compiler has encountered {0} errors",
                    Results.Errors.Count), "Errors while compiling", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                foreach (CompilerError Err in Results.Errors)
                {
                    MessageBox.Show(string.Format("{0}\nLine: {1} - Column: {2}\nFile: {3}", Err.ErrorText,
                        Err.Line, Err.Column, Err.FileName), "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;

            }
            else
            {
                return true;
            }

        }
    }
}
