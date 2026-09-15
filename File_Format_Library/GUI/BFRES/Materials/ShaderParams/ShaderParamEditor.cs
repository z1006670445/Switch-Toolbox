using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bfres.Structs;
using Syroot.NintenTools.NSW.Bfres;
using Toolbox.Library.Forms;
using Toolbox.Library;
using OpenTK;

namespace FirstPlugin.Forms
{
    public partial class ShaderParamEditor : STUserControl
    {
        public ShaderParamEditor()
        {
            InitializeComponent();
        }

        public ImageList il = new ImageList();

        FMAT material;

        /// <summary>
        /// 材质参数名的中文说明，只用于鼠标悬停提示（tooltip）。
        /// 参数名本身是 bfres 文件里的数据，同时被当作 matparam 字典的 key
        /// （见 shaderParamListView_DoubleClick），所以名字不能翻译、不能改名 ——
        /// 中文只走这张表，身份与显示分离。
        /// 主表按 ACNH 实际材质文件整理（mAccessoryAlpha.MatParams.xml 等，
        /// 39 个 Float + 9 个 Float4 + 3 个 TexSrt 全覆盖）。
        /// ⚠️ key 必须与文件里的参数名逐字一致，包括上游的拼写错误
        /// （lgiht / instensity / hair_shift11 等）—— 那是数据里的真实字符串。
        /// </summary>
        static readonly Dictionary<string, string> ParamDescriptions =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ===== 光照 / PBR =====
            { "material_indirect_scale",                        "间接光强度缩放（环境光贡献）" },
            { "material_tc_ambient",                            "材质环境光强度（TEV 环境光）" },
            { "material_roughness",                             "粗糙度（越大越哑光）" },
            { "material_metalness",                             "金属度" },
            { "material_metallic",                              "金属度（旧版写法）" },
            { "material_occlusion",                             "环境光遮蔽（AO）" },
            { "material_fresnel_offset",                        "菲涅尔偏移（边缘反射偏移）" },
            { "material_thickness",                             "材质厚度（透光/次表面用）" },
            { "material_anisotropy",                            "各向异性（拉丝高光）" },
            { "material_emission_intensity",                    "自发光强度" },
            { "material_emission_color",                        "自发光颜色" },
            { "material_specular_intensity",                    "高光强度" },
            { "material_specular_color",                        "高光颜色" },
            { "material_base_color",                            "基础色" },
            { "material_force_alpha",                           "强制 Alpha（忽略贴图透明度）" },
            { "indirect_scale",                                 "间接光缩放（二维）" },
            { "ibl_const_albedo_brightness",                    "IBL 反射率亮度" },
            { "model_env_instance_intensity",                   "环境反射（实例）强度" },
            { "material_game_fill_ratio",                       "补充色（FILL）混合比例" },
            { "material_game_fill_color",                       "补充色（FILL）颜色" },

            // ===== 法线 / 遮罩 =====
            { "material_normal_scale_max",                      "法线贴图最大缩放" },
            { "material_normal_scale_mask",                     "法线缩放遮罩（逐通道）" },
            { "gsys_normalmap2_intensity",                      "法线贴图 2 强度" },
            { "gsys_normalmap2_base_intensity",                 "法线贴图 2 基础强度" },

            // ===== 描边 / Edge Light =====
            { "material_edge_width",                            "描边宽度" },
            { "material_edge_light_intensity",                  "描边光强度" },
            { "enable_edge_light_coordinate_color_constant",    "启用描边光坐标色常量" },
            { "material_edge_lgiht_coordinate_color_brightness","描边光坐标色亮度" },
            { "material_edge_light_specular_width",             "描边光高光宽度" },
            { "material_edge_light_specular_instensity",        "描边光高光强度" },
            { "material_edge_light_coord",                      "描边光坐标" },
            { "material_edge_light_color",                      "描边光颜色" },
            { "enable_edge_light_coord",                        "启用描边光坐标" },
            { "emission_edge_gradation_sharpness",              "自发光描边渐变锐度" },
            { "emission_edge_intensity",                        "自发光描边强度" },

            // ===== 纹理坐标 / 视差 =====
            { "tex_coord0_parallax_shift_value",                "纹理坐标 0 视差偏移" },
            { "tex_coord1_parallax_shift_value",                "纹理坐标 1 视差偏移" },
            { "tex_coord2_parallax_shift_value",                "纹理坐标 2 视差偏移" },
            { "tex_coord1_scale",                               "纹理坐标 1 缩放" },
            { "tex_coord2_scale",                               "纹理坐标 2 缩放" },
            { "tex_srt0",                                       "纹理变换 0（缩放/旋转/平移）" },
            { "tex_srt1",                                       "纹理变换 1（缩放/旋转/平移）" },
            { "tex_srt2",                                       "纹理变换 2（缩放/旋转/平移）" },

            // ===== 摆动动画（植被 / 布料）=====
            { "tree_wave_anim_rotate_x",                        "树木摆动旋转 X（左右摇）" },
            { "tree_wave_anim_rotate_z",                        "树木摆动旋转 Z（前后摇）" },
            { "tree_wave_anim_speed",                           "树木摆动速度" },
            { "tree_wave_anim_distance",                        "树木摆动幅度" },
            { "free_wave_anim_rotate_x",                        "自由摆动旋转 X" },
            { "free_wave_anim_rotate_y",                        "自由摆动旋转 Y" },
            { "free_wave_anim_rotate_z",                        "自由摆动旋转 Z" },
            { "free_wave_anim_scale",                           "自由摆动幅度" },
            { "free_wave_anim_speed",                           "自由摆动速度" },

            // ===== 环境光遮蔽 / 软网格 =====
            { "inside_ao_multiply_intensity",                   "内部 AO 倍增强度" },
            { "soft_mesh_far_range",                            "软网格淡出距离（近镜头透视）" },

            // ===== 水体 =====
            { "water_fog_start_depth",                          "水下雾起始深度" },
            { "water_fog_limit_depth",                          "水下雾极限深度" },
            { "water_fog_limit_alpha",                          "水下雾极限透明度" },
            { "water_edge_limit_depth",                         "水面边缘极限深度" },

            // ===== 透明 / Alpha =====
            { "gsys_alpha_test_ref_value",                      "Alpha 测试参考阈值" },
            { "gsys_xlu_zprepass_alpha",                        "半透明 Z 预通道 Alpha" },
            { "enable_alpha_test",                              "启用透明度测试" },
            { "alpha_test_threshold",                           "透明度测试阈值" },

            // ===== 角色（皮肤 / 头发）=====
            { "material_skin_up_param",                         "皮肤向上渐变参数" },
            { "material_hair_exp0",                             "头发 exp0（发丝参数）" },
            { "material_hair_exp1",                             "头发 exp1（发丝参数）" },
            { "material_hair_shift0",                           "头发 shift0（发丝偏移）" },
            { "material_hair_shift11",                          "头发 shift11（发丝偏移）" },

            // ===== 贴图开关 =====
            { "enable_normal_map",                              "启用法线贴图" },
            { "enable_ao_map",                                  "启用 AO 贴图" },

            // ===== 通用常量槽位（用途由 shader 决定）=====
            { "const_float0",                                   "通用常量浮点 0" },
            { "const_float1",                                   "通用常量浮点 1" },
            { "const_float2",                                   "通用常量浮点 2" },
            { "const_float3",                                   "通用常量浮点 3" },
            { "const_float4",                                   "通用常量浮点 4" },
            { "const_float5",                                   "通用常量浮点 5" },
            { "const_float6",                                   "通用常量浮点 6" },
            { "const_float7",                                   "通用常量浮点 7" },
            { "const_color0",                                   "通用常量颜色 0" },
            { "const_color1",                                   "通用常量颜色 1" },
            { "const_color2",                                   "通用常量颜色 2" },
            { "const_color3",                                   "通用常量颜色 3" },
            { "const_color4",                                   "通用常量颜色 4" },
            { "const_color5",                                   "通用常量颜色 5" },
            { "tev_color0",                                     "TEV 颜色 0" },
            { "tev_color1",                                     "TEV 颜色 1" },
        };

        static string GetParamDescription(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            string desc;
            return ParamDescriptions.TryGetValue(name, out desc) ? desc : "";
        }

        public void InitializeShaderParamList(FMAT mat)
        {
            material = mat;

            int CurParam = 0;

            shaderParamListView.Items.Clear();

            // 开启逐项 tooltip：参数的中文说明走悬停显示，不额外占用列宽。
            // 显式设一次（虽然 ListView 该属性默认为 true，但 Designer 里没写，
            // 且 ToolTipText 只有在它为 true 时才会被使用）——避免依赖默认值。
            shaderParamListView.ShowItemToolTips = true;

            foreach (BfresShaderParam prm in mat.matparam.Values)
            {
                var item = new ListViewItem(prm.Name);
                ShaderParamToListItem(prm, item);

                shaderParamListView.View = View.Details;
                shaderParamListView.Items.Add(item);
                CurParam++;
            }
            il.ImageSize = new Size(10, 10);
            shaderParamListView.SmallImageList = il;
            shaderParamListView.FullRowSelect = true;
        }

        private void ShaderParamToListItem(BfresShaderParam prm, ListViewItem item )
        {
            item.SubItems.Clear();
            item.Text = prm.Name;

            string DisplayValue = "";

            switch (prm.Type)
            {
                case ShaderParamType.Int:
                case ShaderParamType.Int2:
                case ShaderParamType.Int3:
                case ShaderParamType.Int4:
                    DisplayValue = SetValueToString(prm.ValueInt);
                    break;
                case ShaderParamType.UInt:
                case ShaderParamType.UInt2:
                case ShaderParamType.UInt3:
                case ShaderParamType.UInt4:
                    DisplayValue = SetValueToString(prm.ValueUint);
                    break;
                case ShaderParamType.Float:
                case ShaderParamType.Float2:
                case ShaderParamType.Float2x2:
                case ShaderParamType.Float2x3:
                case ShaderParamType.Float2x4:
                case ShaderParamType.Float3x2:
                case ShaderParamType.Float3x3:
                case ShaderParamType.Float3x4:
                case ShaderParamType.Float4x2:
                case ShaderParamType.Float4x3:
                case ShaderParamType.Float4x4:
                    DisplayValue = SetValueToString(prm.ValueFloat);
                    break;
                case ShaderParamType.Float3:
                    DisplayValue = SetValueToString(prm.ValueFloat);
                    break;
                case ShaderParamType.Float4:
                    DisplayValue = SetValueToString(prm.ValueFloat);
                    break;
                case ShaderParamType.TexSrt:
                    DisplayValue = SetValueToString(prm.ValueTexSrt);
                    break;
                case ShaderParamType.TexSrtEx:
                    DisplayValue = SetValueToString(prm.ValueTexSrtEx);
                    break;
                case ShaderParamType.Srt2D:
                    DisplayValue = SetValueToString(prm.ValueSrt2D);
                    break;
            }

            item.UseItemStyleForSubItems = false;
            item.SubItems.Add(DisplayValue);
            item.SubItems.Add("");
            item.SubItems[2].BackColor = GetColor(prm);

            // 中文说明走鼠标悬停提示（需配合 shaderParamListView.ShowItemToolTips = true）。
            // 参数名不可改名（是文件数据 + matparam 的 key），所以身份与显示分离：
            // 名称列照旧显示原文，中文只在悬停时给出。
            // 未收录的参数名只提示完整参数名 —— 名称列宽 94px，长参数名会被截断。
            string desc = GetParamDescription(prm.Name);
            item.ToolTipText = desc.Length > 0 ? prm.Name + "\n" + desc : prm.Name;
        }

        private Color GetColor(BfresShaderParam prm)
        {
            Vector4 col = new Vector4();

            switch (prm.Type)
            {
                case ShaderParamType.Float3:
                    col = new Vector4(prm.ValueFloat[0], prm.ValueFloat[1], prm.ValueFloat[2], 1);
                    break;
                case ShaderParamType.Float4:
                    col = new Vector4(prm.ValueFloat[0], prm.ValueFloat[1], prm.ValueFloat[2], prm.ValueFloat[3]);
                    break;
            }

            bool IsColor = prm.Name.Contains("Color") ||
                             prm.Name.Contains("color") ||
                             prm.Name.Contains("konst0") ||
                             prm.Name.Contains("konst1") ||
                             prm.Name.Contains("konst2") ||
                             prm.Name.Contains("konst3");

            Color SetColor = FormThemes.BaseTheme.ListViewBackColor;

            if (IsColor)
            {
                SetColor = Color.FromArgb(
                255,
                Utils.FloatToIntClamp(col.X),
                Utils.FloatToIntClamp(col.Y),
                Utils.FloatToIntClamp(col.Z)
                );
            }

            return SetColor;
        }

        private string SetValueToString(object values)
        {
            if (values is float[])
                return string.Join(" , ", values as float[]);
            else if (values is TexSrt)
                return TexSrtToString((TexSrt)values);
            else if (values is TexSrtEx)
                return TexSrtToString((TexSrtEx)values);
            else if (values is Srt2D)
                return TexSrtToString((Srt2D)values);
            else if (values is bool[])
                return string.Join(" , ", values as bool[]);
            else if (values is int[])
                return string.Join(" , ", values as int[]);
            else if (values is uint[])
                return string.Join(" , ", values as uint[]);
            else
                return "";
        }

        private string TexSrtToString(TexSrtEx val)
        {
            return $"{val.Mode} {val.Scaling.X} {val.Scaling.Y} {val.Rotation} {val.Translation.X} {val.Translation.Y}  ";
        }

        private string TexSrtToString(TexSrt val)
        {
            return $"{val.Mode} {val.Scaling.X} {val.Scaling.Y} {val.Rotation} {val.Translation.X} {val.Translation.Y}  ";
        }

        private string TexSrtToString(Srt2D val)
        {
            return $"{val.Scaling.X} {val.Scaling.Y} {val.Rotation} {val.Translation.X} {val.Translation.Y}  ";
        }

        STFlowLayoutPanel stFlowLayoutPanel1;
        private void LoadDropDownPanel(BfresShaderParam param)
        {
            stFlowLayoutPanel1 = new STFlowLayoutPanel();
            stFlowLayoutPanel1.Dock = DockStyle.Fill;
            stFlowLayoutPanel1.SuspendLayout();

            if (param.Type == ShaderParamType.Bool ||
               param.Type == ShaderParamType.Bool2 ||
               param.Type == ShaderParamType.Bool3 ||
               param.Type == ShaderParamType.Bool4)
            {
                booleanPanel panel = new booleanPanel(param.ValueBool, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.TexSrtEx)
            {
                TexSrtPanel panel = new TexSrtPanel(param.ValueTexSrtEx, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.TexSrt)
            {
                TexSrtPanel panel = new TexSrtPanel(param.ValueTexSrt, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.Srt2D)
            {
                TexSrtPanel2D panel = new TexSrtPanel2D(param.ValueSrt2D, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.Float)
            {
                vector1SliderPanel panel = new vector1SliderPanel(param.ValueFloat, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.Float2)
            {
                vector2SliderPanel panel = new vector2SliderPanel(param.ValueFloat, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.Float3)
            {
                vector3SliderPanel panel = new vector3SliderPanel(param.Name, param.ValueFloat, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.Float4)
            {
                vector4SliderPanel panel = new vector4SliderPanel(param.Name, param.ValueFloat, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.UInt)
            {
                vector1SliderPanel panel = new vector1SliderPanel(param.ValueUint, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.UInt2)
            {
                vector2SliderPanel panel = new vector2SliderPanel(param.ValueUint, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.UInt3)
            {
                vector3SliderPanel panel = new vector3SliderPanel(param.Name, param.ValueUint, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.UInt4)
            {
                vector4SliderPanel panel = new vector4SliderPanel(param.Name, param.ValueUint, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.Int)
            {
                vector1SliderPanel panel = new vector1SliderPanel(param.ValueInt, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.Int2)
            {
                vector2SliderPanel panel = new vector2SliderPanel(param.ValueInt, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.Int3)
            {
                vector3SliderPanel panel = new vector3SliderPanel(param.Name, param.ValueInt, param);
                LoadDropPanel(panel, param);
            }
            else if (param.Type == ShaderParamType.Int4)
            {
                vector4SliderPanel panel = new vector4SliderPanel(param.Name, param.ValueInt, param);
                LoadDropPanel(panel, param);
            }

            stFlowLayoutPanel1.ResumeLayout();
        }

        public bool OnValueChanged(BfresShaderParam param, UserControl sender)
        {
            if (param == null || sender.Parent == null)
                return false;

            STDropDownPanel panel = (STDropDownPanel)sender.Parent;
            panel.PanelValueName = GetValueString(param);


            panel.Refresh();

            return true;
        }

        public void LoadDialogDropPanel(ParamValueDialog control, BfresShaderParam param)
        {
            ParamValueEditorBase panel = new ParamValueEditorBase();

            switch (param.Type)
            {
                case ShaderParamType.Float: panel = new vector1SliderPanel(param.ValueFloat, param); break;
                case ShaderParamType.Float2: panel = new vector2SliderPanel(param.ValueFloat, param); break;
                case ShaderParamType.Float3: panel = new vector3SliderPanel(param.Name, param.ValueFloat, param); break;
                case ShaderParamType.Float4: panel = new vector4SliderPanel(param.Name, param.ValueFloat, param); break;
                case ShaderParamType.Int: panel = new vector1SliderPanel(param.ValueInt, param); break;
                case ShaderParamType.Int2: panel = new vector2SliderPanel(param.ValueInt, param); break;
                case ShaderParamType.Int3: panel = new vector3SliderPanel(param.Name, param.ValueInt, param); break;
                case ShaderParamType.Int4: panel = new vector4SliderPanel(param.Name, param.ValueInt, param); break;
                case ShaderParamType.UInt: panel = new vector1SliderPanel(param.ValueUint, param); break;
                case ShaderParamType.UInt2: panel = new vector2SliderPanel(param.ValueUint, param); break;
                case ShaderParamType.UInt3: panel = new vector3SliderPanel(param.Name, param.ValueUint, param); break;
                case ShaderParamType.UInt4: panel = new vector4SliderPanel(param.Name, param.ValueUint, param); break;
                case ShaderParamType.TexSrt: panel = new TexSrtPanel(param.ValueTexSrt,param); break;
                case ShaderParamType.TexSrtEx: panel = new TexSrtPanel(param.ValueTexSrtEx, param); break;
                case ShaderParamType.Srt2D: panel = new TexSrtPanel2D(param.ValueSrt2D, param); break;
                case ShaderParamType.Bool: panel = new booleanPanel(param.ValueBool, param); break;
                case ShaderParamType.Bool2: panel = new booleanPanel(param.ValueBool, param); break;
                case ShaderParamType.Bool3: panel = new booleanPanel(param.ValueBool, param); break;
                case ShaderParamType.Bool4: panel = new booleanPanel(param.ValueBool, param); break;
            }
            control.Width = panel.Width;
            control.Height = panel.Height + 70;
            control.CanResize = false;
            control.BackColor = FormThemes.BaseTheme.DropdownPanelBackColor;
            control.AddControl(panel);
        }

        public void LoadDropPanel(ParamValueEditorBase control, BfresShaderParam param)
        {
            STDropDownPanel panel = new STDropDownPanel();
            panel.SuspendLayout();
            panel.PanelName = param.Name;
            panel.PanelValueName = GetValueString(param);
            panel.Controls.Add(control);
            panel.Height = control.Height;
            panel.IsExpanded = false;

            control.BackColor = FormThemes.BaseTheme.DropdownPanelBackColor;
            control.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left;
            control.Width = panel.Width;
            control.LoadAction(OnValueChanged); //To update value test

            if (control is vector4SliderPanel)
                panel.SetIconColor = ((vector4SliderPanel)control).GetColor();
            if (control is vector4SliderPanel)
                panel.SetIconAlphaColor = ((vector4SliderPanel)control).GetAlphaColor();
            if (control is vector3SliderPanel)
                panel.SetIconColor = ((vector3SliderPanel)control).GetColor();

            panel.ResumeLayout();

            stFlowLayoutPanel1.Controls.Add(panel);
        }

        private string GetValueString(BfresShaderParam param)
        {
            string Values = "";

            switch (param.Type)
            {

                case ShaderParamType.Float:
                    Values = $"[ {RoundParam(param.ValueFloat[0])} ]";
                    break;
                case ShaderParamType.Float2:
                    Values = $"[ {RoundParam(param.ValueFloat[0])} ," +
                              $" {RoundParam(param.ValueFloat[1])} ]";
                    break;
                case ShaderParamType.Float3:
                    Values = $"[ {RoundParam(param.ValueFloat[0])} ," +
                              $" {RoundParam(param.ValueFloat[1])} ," +
                             $"  {RoundParam(param.ValueFloat[2])} ,]";
                    break;
                case ShaderParamType.Float4:
                    Values = $"[ {RoundParam(param.ValueFloat[0])} ," +
                              $" {RoundParam(param.ValueFloat[1])} ," +
                             $"  {RoundParam(param.ValueFloat[2])} ," +
                             $"  {RoundParam(param.ValueFloat[3])} ]";
                    break;
                case ShaderParamType.TexSrt:
                    Values = $"[ {param.ValueTexSrt.Mode} ," +
                             $" {RoundParam(param.ValueTexSrt.Scaling.X)} ," +
                             $" {RoundParam(param.ValueTexSrt.Scaling.Y)} ," +
                             $" {RoundParam(param.ValueTexSrt.Rotation)}, " +
                             $" {RoundParam(param.ValueTexSrt.Translation.X)}," +
                             $" {RoundParam(param.ValueTexSrt.Translation.X)} ]";
                    break;
            }

            Console.WriteLine(String.Format("{0,-30} {1,-30}", param.Name, Values));

            return Values;
        }

        private float RoundParam(float Value) {
            return (float)Math.Round(Value, 2);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Material Params|*.xml;";
            sfd.DefaultExt = ".xml";
            sfd.FileName = material.Text + ".MatParams";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                FMAT2XML.Save(material, sfd.FileName, true);
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Material Params|*.xml;";
            ofd.DefaultExt = ".xml";
            ofd.FileName = material.Text + ".MatParams";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                FMAT2XML.Read(material, ofd.FileName, true);
            }
        }

        private void shaderParamListView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void shaderParamListView_Click(object sender, EventArgs e)
        {
          
        }

        private void shaderParamListView_DoubleClick(object sender, EventArgs e)
        {
            if (shaderParamListView.SelectedItems.Count > 0)
            {
                var currentItem = shaderParamListView.SelectedItems[0];

                if (material.matparam.ContainsKey(currentItem.Text))
                {
                    ParamValueDialog dialog = new ParamValueDialog();
                    LoadDialogDropPanel(dialog, material.matparam[currentItem.Text]);
                    dialog.Location = currentItem.Position;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        ShaderParamToListItem(material.matparam[currentItem.Text], shaderParamListView.SelectedItems[0]);
                    }
                }
            }
        }
    }
}
