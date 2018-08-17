using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Capillary
{
    public class CapillaryComponent : GH_Component
    {
        private List<GH_Point> gh_startingWaterPoints;
        private List<GH_Point> gh_startingPlantPoints;
        private GH_Mesh gh_terrain;
        private GH_Number gh_resolution;
        private GH_Rectangle gh_boundary;
        private GH_Boolean gh_reset;
        private GH_Boolean gh_run;

        private List<Point3d> startingWaterPoints;
        private List<Point3d> startingPlantPoints;
        private Mesh terrain;
        private double resolution;
        private Rectangle3d boundary;

        private WorldMap world;

        private GH_Structure<GH_Point> gh_roots;
        private GH_Mesh gh_rootMesh;
        private List<GH_Number> plant_values;
        private List<GH_Number> water_values;
        private List<GH_Point> gh_turtles;
        private GH_String gh_log;

        private Point3d[,] roots;
        private Mesh rootMesh;
        private String log;

        private System.Diagnostics.Stopwatch sw;

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CapillaryComponent()
          : base("Capillary", "Capillary",
              "Plant growth simulator for transport networks",
              "Extra", "Growth")
        {
            gh_startingPlantPoints = new List<GH_Point>();
            gh_startingWaterPoints = new List<GH_Point>();

            gh_roots = new GH_Structure<GH_Point>();

            sw = new System.Diagnostics.Stopwatch();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("WaterPoints", "WP", "generic description", GH_ParamAccess.list);
            pManager.AddPointParameter("PlantPoints", "PP", "generic description", GH_ParamAccess.list);
            pManager.AddMeshParameter("Terrain", "T", "generic description", GH_ParamAccess.item);
            pManager.AddNumberParameter("Resolution", "Res", "generic description", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Boundary", "Bnd", "generic description", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Reset", "Reset", "Link button here", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Link Toggle here", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Roots", "R", "generic description", GH_ParamAccess.tree);
            pManager.AddMeshParameter("RootMesh", "M", "generic description", GH_ParamAccess.item);
            pManager.AddNumberParameter("plant_value", "P", "generic description", GH_ParamAccess.list);
            pManager.AddNumberParameter("water value", "W", "water", GH_ParamAccess.list);
            pManager.AddPointParameter("turtles", "T", "Tertues", GH_ParamAccess.list);
            pManager.AddTextParameter("log" ,"L", "output log", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            log = "";

            DA.GetData(5, ref gh_reset);
            if (gh_reset.Value)
            {
                DA.GetDataList(0, gh_startingWaterPoints);
                DA.GetDataList(1, gh_startingPlantPoints);
                DA.GetData(2, ref gh_terrain);
                DA.GetData(3, ref gh_resolution);
                DA.GetData(4, ref gh_boundary);

                startingWaterPoints = (from GH_Point gh_Pt in gh_startingWaterPoints select gh_Pt.Value).ToList();
                startingPlantPoints = (from GH_Point gh_Pt in gh_startingPlantPoints select gh_Pt.Value).ToList();
                terrain = gh_terrain.Value;
                resolution = gh_resolution.Value;
                boundary = gh_boundary.Value;

                world = new WorldMap(boundary, resolution, terrain, startingWaterPoints, startingPlantPoints);
                roots = new Point3d[world.plantMap.xLen, world.plantMap.yLen];

                log += "World successfully created.";
            }

            DA.GetData(6, ref gh_run);
            if (gh_run.Value)
            {
                sw.Start();
                world.Tick();
                sw.Stop();

                log += $"world step {world.step}: took {sw.Elapsed}.";
                for (int j = 0; j < world.plantMap.yLen; j++)
                {
                    for (int i = 0; i < world.plantMap.xLen; i++)
                    {
                        roots[i, j] = world.plantMap.Points[i, j];
                        roots[i, j].Z = world.plantMap[i, j].val;
                    }
                }

                if (gh_roots.IsEmpty)
                {
                    GH_Path path = new GH_Path(0, 0, 0);
                    for (int i = 0; i < roots.GetLength(0); i++)
                    {
                        for (int j = 0; j < roots.GetLength(1); j++)
                        {
                            gh_roots.Append(new GH_Point(roots[i, j]), path);
                        }
                        path = path.Increment(2);
                    }
                }
                else
                {
                    for (int i = 0; i < world.plantMap.xLen; i++)
                    {
                        for (int j = 0; j < world.plantMap.yLen; j++)
                        {
                            gh_roots[new GH_Path(0, 0, i)][j] = new GH_Point(roots[i, j]);
                        }
                    }
                }

                if (rootMesh == null)
                {
                    //rootMesh = Mesh.CreateFromPlane(world.boundary.Plane, 
                    //    world.boundary.X, world.boundary.Y, world.plantMap.xLen, world.plantMap.yLen);
                    rootMesh = new Mesh();
                    double X_size = world.boundary.Width / world.plantMap.xLen;
                    double Y_size = world.boundary.Height / world.plantMap.yLen;

                    for (int j = 0; j < world.plantMap.yLen; j++)
                    {
                        for (int i = 0; i < world.plantMap.xLen; i++)
                        {
                           rootMesh.Vertices.Add(world.plantMap.Points[i, j]);
                        }
                    }

                    for (int j = 0; j < world.plantMap.yLen - 1; j++)
                    {
                        for (int i = 0; i < world.plantMap.xLen - 1; i++)
                        {
                            int v1 = j * (world.plantMap.xLen) + i;
                            int v2 = j * (world.plantMap.xLen) + i + 1;
                            int v3 = (j + 1) * (world.plantMap.xLen) + i + 1;
                            int v4 = (j + 1) * (world.plantMap.xLen) + i;

                            rootMesh.Faces.AddFace(v1, v2, v3, v4);
                        }
                    }
                }

                List<double> p_vals = new List<double>();
                for (int j = 0; j < world.plantMap.yLen; j++)
                    for (int i = 0; i < world.plantMap.xLen; i++)
                    {
                        p_vals.Add(world.plantMap[i, j].val);
                    }

                List<double> w_vals = new List<double>();
                for (int j = 0; j < world.waterMap.yLen; j++)
                    for (int i = 0; i < world.waterMap.xLen; i++)
                    {
                        w_vals.Add(world.waterMap[i, j].val);
                    }

                plant_values = (from v in p_vals select new GH_Number(v)).ToList();
                water_values = (from v in w_vals select new GH_Number(v)).ToList();
                gh_turtles = (from t in world.plant_turtles
                              select new GH_Point(world.plantMap.Points[t.location_x, t.location_y])).ToList();

                Interval val_interval = new Interval(p_vals.Min(), p_vals.Max());
                double plantHue = 115.0;

                //normalize
                p_vals = (from v in p_vals select val_interval.NormalizedParameterAt(v)).ToList();

                Interval new_interval = new Interval(0.3, 0.9);
                //remap
                p_vals = (from v in p_vals select new_interval.ParameterAt(v)).ToList();

                for(int i = 0; i < rootMesh.VertexColors.Count; i++)
                {
                    HSLColor hsl = new HSLColor(plantHue, 1.0, p_vals[i]);
                    rootMesh.VertexColors[i] = hsl;
                }

                gh_rootMesh = new GH_Mesh(rootMesh);

                gh_log = new GH_String(log);
            }

            DA.SetDataTree(0, gh_roots);
            DA.SetData(1, gh_rootMesh);
            DA.SetDataList(2, plant_values);
            DA.SetDataList(3, water_values);
            DA.SetDataList(4, gh_turtles);
            DA.SetData(5, gh_log);
        }
         
        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0b9918c4-2657-4c2c-b0c1-2f2d73087183"); }
        }

        //utility class for HSL

        public class HSLColor
        {
            // Private data members below are on scale 0-1
            // They are scaled for use externally based on scale
            private double hue = 1.0;
            private double saturation = 1.0;
            private double luminosity = 1.0;

            private const double scale = 240.0;

            public double Hue
            {
                get { return hue * scale; }
                set { hue = CheckRange(value / scale); }
            }
            public double Saturation
            {
                get { return saturation * scale; }
                set { saturation = CheckRange(value / scale); }
            }
            public double Luminosity
            {
                get { return luminosity * scale; }
                set { luminosity = CheckRange(value / scale); }
            }

            private double CheckRange(double value)
            {
                if (value < 0.0)
                    value = 0.0;
                else if (value > 1.0)
                    value = 1.0;
                return value;
            }

            public override string ToString()
            {
                return String.Format("H: {0:#0.##} S: {1:#0.##} L: {2:#0.##}", Hue, Saturation, Luminosity);
            }

            public string ToRGBString()
            {
                Color color = (Color)this;
                return String.Format("R: {0:#0.##} G: {1:#0.##} B: {2:#0.##}", color.R, color.G, color.B);
            }

            #region Casts to/from System.Drawing.Color
            public static implicit operator Color(HSLColor hslColor)
            {
                double r = 0, g = 0, b = 0;
                if (hslColor.luminosity != 0)
                {
                    if (hslColor.saturation == 0)
                        r = g = b = hslColor.luminosity;
                    else
                    {
                        double temp2 = GetTemp2(hslColor);
                        double temp1 = 2.0 * hslColor.luminosity - temp2;

                        r = GetColorComponent(temp1, temp2, hslColor.hue + 1.0 / 3.0);
                        g = GetColorComponent(temp1, temp2, hslColor.hue);
                        b = GetColorComponent(temp1, temp2, hslColor.hue - 1.0 / 3.0);
                    }
                }
                return Color.FromArgb((int)(255 * r), (int)(255 * g), (int)(255 * b));
            }

            private static double GetColorComponent(double temp1, double temp2, double temp3)
            {
                temp3 = MoveIntoRange(temp3);
                if (temp3 < 1.0 / 6.0)
                    return temp1 + (temp2 - temp1) * 6.0 * temp3;
                else if (temp3 < 0.5)
                    return temp2;
                else if (temp3 < 2.0 / 3.0)
                    return temp1 + ((temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0);
                else
                    return temp1;
            }
            private static double MoveIntoRange(double temp3)
            {
                if (temp3 < 0.0)
                    temp3 += 1.0;
                else if (temp3 > 1.0)
                    temp3 -= 1.0;
                return temp3;
            }
            private static double GetTemp2(HSLColor hslColor)
            {
                double temp2;
                if (hslColor.luminosity < 0.5)  //<=??
                    temp2 = hslColor.luminosity * (1.0 + hslColor.saturation);
                else
                    temp2 = hslColor.luminosity + hslColor.saturation - (hslColor.luminosity * hslColor.saturation);
                return temp2;
            }

            public static implicit operator HSLColor(Color color)
            {
                HSLColor hslColor = new HSLColor
                {
                    hue = color.GetHue() / 360.0, // we store hue as 0-1 as opposed to 0-360 
                    luminosity = color.GetBrightness(),
                    saturation = color.GetSaturation()
                };
                return hslColor;
            }
            #endregion

            public void SetRGB(int red, int green, int blue)
            {
                HSLColor hslColor = (HSLColor)Color.FromArgb(red, green, blue);
                this.hue = hslColor.hue;
                this.saturation = hslColor.saturation;
                this.luminosity = hslColor.luminosity;
            }

            public HSLColor() { }
            public HSLColor(Color color)
            {
                SetRGB(color.R, color.G, color.B);
            }
            public HSLColor(int red, int green, int blue)
            {
                SetRGB(red, green, blue);
            }
            public HSLColor(double hue, double saturation, double luminosity)
            {
                Hue = hue;
                Saturation = saturation;
                Luminosity = luminosity;
            }
        }
    }
}
