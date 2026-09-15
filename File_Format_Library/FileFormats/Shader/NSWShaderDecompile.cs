using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using Toolbox.Library;

namespace FirstPlugin
{
    public class NSWShaderDecompile
    {
        public enum NswShaderType
        {
            Vertex,
            Geometry,
            Fragment,
            Compute
        }

        public static string DecompileShader(NswShaderType shaderType, byte[] Data, ulong Address = 0)
        {
            if (!Directory.Exists("temp"))
                Directory.CreateDirectory("temp");

            if (!Directory.Exists("ShaderTools"))
                Directory.CreateDirectory("ShaderTools");

            //     File.WriteAllBytes("temp/shader1.bin", Utils.CombineByteArray(data.ToArray()));
            File.WriteAllBytes("temp/shader1.bin", Data);

            if (!File.Exists($"{Runtime.ExecutableDir}/ShaderTools/Ryujinx.ShaderTools.exe"))
            {
                MessageBox.Show("ShaderTools 中未找到 Shader 反编译器。如需反编译 Shader，可使用 Ryujinx 的 ShaderTools，并放入 Toolbox 的 ShaderTools 文件夹。");
                return "";
            }

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "ShaderTools/Ryujinx.ShaderTools.exe";
            start.WorkingDirectory = Runtime.ExecutableDir;
            start.Arguments = $"{Utils.AddQuotesIfRequired("temp/shader1.bin")}";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            start.WindowStyle = ProcessWindowStyle.Hidden;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    try
                    {
                        return reader.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        Toolbox.Library.Forms.STErrorDialog.Show("Failed to decompile shader!", "Shader Tools", ex.ToString());
                        return "";
                    }
                }
            }
        }
    }
}
