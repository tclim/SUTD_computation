using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace Capillary
{
    public class WorldMap
    {
        //state machine(?) stuff
        public int step;

        //inherited by PatchMaps
        public double resolution;
        public Rectangle3d boundary;

        //put the different layers here
        public TerrainMap terrainMap;
        public WaterMap waterMap;
        public PlantMap plantMap;
        public List<Plant_Turtle> plant_turtles = new List<Plant_Turtle>();


        public WorldMap(Rectangle3d _boundary, double _resolution, Mesh _m, List<Point3d> _waterSource, List<Point3d> _plantSource)
        {
            step = 0;

            resolution = _resolution;
            _boundary.RecenterPlane(0);
            boundary = _boundary;

            //register your map here
            terrainMap = new TerrainMap(this, _m);
            waterMap = new WaterMap(this, _waterSource);
            plantMap = new PlantMap(this, _plantSource);
            Random rnd = new Random();
            for (int no_turtles = 0; no_turtles < _plantSource.Count(); no_turtles++)
            {
                int locX = (int)_plantSource[no_turtles].X;
                int locY = (int)_plantSource[no_turtles].Y;
                int Xmax = plantMap.xLen - 1;
                int Ymax = plantMap.yLen - 1;
                locX = locX < 0 ? 0 : locX > Xmax ? Xmax : locX;
                locY = locY < 0 ? 0 : locY > Ymax ? Ymax : locY;
                plant_turtles.Add(new Plant_Turtle(locX, locY, rnd.NextDouble() * 360.0, this));
            }
            


        }

        public void Tick()
        {
            terrainMap.Tick();
            waterMap.Tick();
            Spawn();
            MoveTurtles();
            Live();
            plantMap.Tick();
            step++;
        }

        public void Live()
        {
            foreach (Plant_Turtle individual_turtle in plant_turtles)
            {
                int locx = individual_turtle.location_x;
                int locy = individual_turtle.location_y;

                if (waterMap[locx, locy].val > 20 || waterMap[locx, locy].val < 1)
                {
                    individual_turtle.alive = false;

                }
            }
        }

        public void Spawn()
        {
            if (plant_turtles.Count() > 500) return;
            Random rnd = new Random();
            List<Plant_Turtle> new_turtles = new List<Plant_Turtle>();
            foreach (Plant_Turtle individual_turtle in plant_turtles)
            {
                int locx = individual_turtle.location_x;
                int locy = individual_turtle.location_y;
                if (waterMap[locx, locy].val < 20 && waterMap[locx, locy].val > 5)
                {
                    Plant_Turtle newplant = new Plant_Turtle(locx, locy, rnd.NextDouble() * 360.0, this);
                    new_turtles.Add(newplant);
                }
            }
            plant_turtles.AddRange(new_turtles);
        }

        public void MoveTurtles()
        {
            List<Plant_Turtle> alive_turtles = (from turtle in plant_turtles where turtle.alive select turtle).ToList();
            plant_turtles = alive_turtles;

            foreach (Plant_Turtle individual in plant_turtles) // still need to add in the alive function above
            {
                //int counter = 0;
                //double valuex = 0.0;
                //double valuey = 0.0;
                //double factorplant = 1.0;
                //double factorwater = 2.0;
                //int originx = individual.location_x;
                //int originy = individual.location_y;
                //double weightages = 0.0;

                //for (int xcor = originx - (plantMap.xLen/5); xcor < originx + (plantMap.xLen / 5); xcor++)
                //{
                //    if (xcor < plantMap.yLen - 1 && xcor > 0 && xcor != originx)
                //    {
                //        for (int ycor = originy - (plantMap.xLen / 5); ycor < originy + (plantMap.xLen / 5); ycor++)
                //        {
                //            if (ycor < plantMap.yLen - 1 && ycor > 0 && ycor != originy)
                //            {
                //                double distancesquare = Math.Pow(xcor - originx, 2) + Math.Pow(ycor - originy, 2);
                //                valuex += factorplant * (1.0 / distancesquare) *(plantMap[xcor,ycor].val* factorplant + waterMap[xcor, ycor].val * factorwater) *(xcor - originx);
                //                valuey += factorplant * (1.0 / distancesquare) * (plantMap[xcor, ycor].val * factorplant + waterMap[xcor, ycor].val * factorwater) * (ycor - originy);
                //                weightages += (plantMap[xcor, ycor].val + waterMap[xcor, ycor].val);
                //                counter += 1;
                //            }

                //        }
                //    }
                    
                //}

                //Are conditions to find new target met?
                if(individual.target_x == -1 || individual.target_y == -1 || (individual.target_x == individual.location_x && individual.target_y == individual.location_y))
                {
                    int tx, ty;
                    Plant_Turtle.FindNewTarget(waterMap.bestWater, out tx, out ty);
                    individual.target_x = tx;
                    individual.target_y = ty;
                }

                //Throw dice to determine if individual should head straight for the target or move towards a patch.
                Random r = new Random(15528);
                if (r.NextDouble() < Plant_Turtle.patchBias)
                    individual.MoveTowardsPatch(plantMap.desired_plants);
                else
                    individual.MoveTowardsTarget();

                //Vector2d direction_vector = new Vector2d(valuex / weightages, valuey / counter); //directional vector to face
                //if (plantMap.desired_plants.Count() > 0)
                //{
                //    for (int patch = 0; patch<plantMap.desired_plants.Count(); patch++)
                //    {
                //        direction_vector.X += .5 * (plantMap.desired_plants[patch].locX-originx);
                //        direction_vector.Y += 5 * (plantMap.desired_plants[patch].locY-originy);
                //    }
                //}
                //double initial_heading = Math.Atan(direction_vector.X/direction_vector.Y);
                //direction_vector.Unitize();
                //double locX = individual.location_x;
                //double locY = individual.location_y;
                //locX += direction_vector.X;
                //locY += direction_vector.Y;
                //locX = Math.Round(locX);
                //locY = Math.Round(locY);

                //individual.location_x = (int)locX;
                //individual.location_y = (int)locY;

                //if (individual.location_x < 0)
                //{
                //    individual.location_x = 0;
                //}
                //if (individual.location_x > plantMap.xLen-1)
                //{
                //    individual.location_x = plantMap.xLen - 1;
                //}
                //if (individual.location_y < 0)
                //{
                //    individual.location_y = 0;
                //}
                //if (individual.location_y > plantMap.yLen - 1)
                //{
                //    individual.location_y = plantMap.yLen - 1;
                //}
            }

        }
    }

    //(also as an example of creating a new class of patchmaps)
    //remember to inherit from PatchMap<Patch>
    //which will already come with a MapArray and a PointArray initialized for your usage from the get-go
    public class TerrainMap
    {
        public Terrain[,] Map { get; }
        public Point3d[,] Points;
        public WorldMap World { get; }
        public Terrain this[int x, int y]
        {
            get { return Map[x, y]; }
            set { Map[x, y] = value; }
        }
        public int xLen;
        public int yLen;

        public TerrainMap(WorldMap _world, Mesh m)
        {
            World = _world;

            int sz_X = (int)(World.boundary.Width / World.resolution);
            int sz_Y = (int)(World.boundary.Height / World.resolution);
            double resX = World.boundary.Width / sz_X;
            double resY = World.boundary.Height / sz_Y;


            Points = new Point3d[sz_X, sz_Y];
            Map = new Terrain[sz_X, sz_Y];
            xLen = sz_X;
            yLen = sz_Y;
            //divide the rectangle into little rectangles, and assign location to be the midpoint of those rectangles.
            //set the locX, locY, value appropriately for MapArray 
            for (int y = 0; y < sz_Y; y++)
            {
                for (int x = 0; x < sz_X; x++)
                {
                    Points[x, y] = World.boundary.Plane.Origin +
                        (0.5 + x) * resX * World.boundary.Plane.XAxis +
                        (0.5 + y) * resY * World.boundary.Plane.YAxis;
                    Map[x, y] = new Terrain(this);
                    Map[x, y].locX = x;
                    Map[x, y].locY = y;
                    Map[x, y].val = 0;
                    Map[x, y].neighbours = new List<Terrain>();
                }
            }

            //store neighbours
            for (int x = 0; x < sz_X; x++)
            {
                for (int y = 0; y < sz_Y; y++)
                {
                    Map[x, y].GenerateNeighbours();
                }
            }

            //from here do stuff that is specific to the implementation. In this case
            //terrain will change the values in the mapArray accordingly to reflect the landscape
            //of the mesh passed to it in the constructor.
            double safeHeight = m.GetBoundingBox(true).Max.Z + 10.0;
            for (int x = 0; x < xLen; x++)
            {
                for (int y = 0; y < yLen; y++)
                {
                    Point3d point = Points[x, y];
                    point.Z = safeHeight;
                    Ray3d ray = new Ray3d(point, -Vector3d.ZAxis);
                    double t = Rhino.Geometry.Intersect.Intersection.MeshRay(m, ray);
                    Point3d projected = ray.PointAt(t);
                    Map[x, y].val = projected.Z;
                }
            }
        }

        public Tuple<int, int> ClosestCoords(Point3d point)
        {
            PointCloud pointCloud = new PointCloud();
            for (int y = 0; y < yLen; y++)
            {
                for (int x = 0; x < xLen; x++)
                {
                    pointCloud.Add(Points[x, y]);
                }
            }

            int i = pointCloud.ClosestPoint(point);
            return Tuple.Create(i % xLen, i / xLen);
        }

        public void Tick()
        {

        }
    }

    public class Terrain
    {
        public int locX;
        public int locY;
        public double val;
        public TerrainMap Map;
        public List<Terrain> neighbours;

        public Terrain()
        {

        }
        public Terrain(TerrainMap terrainMap)
        {
            Map = terrainMap ?? throw new ArgumentNullException("terrainMap");
        }

        static public Terrain Invalid
        {
            get
            {
                return new Terrain
                {
                    locX = int.MaxValue,
                    locY = int.MaxValue,
                    val = double.PositiveInfinity
                };
            }
        }

        public void GenerateNeighbours()
        {
            List<int> x_offsets = new List<int> { -1, -1, 0, 1, 1, 1, 0, -1 };
            List<int> y_offsets = new List<int> { 0, 1, 1, 1, 0, -1, -1, -1 };

            for (int i = 0; i < 8; i++)
            {
                int x = locX + x_offsets[i];
                int y = locY + y_offsets[i];

                if (x < 0 || x > Map.xLen - 1 ||
                    y < 0 || y > Map.yLen - 1)
                    neighbours.Add(Invalid);
                else
                    neighbours.Add(Map[x, y]);
            }
        }
    }

    public class WaterMap
    {
        public Water[,] Map { get; }
        public Point3d[,] Points { get; }
        public WorldMap World { get; }
        public Water this[int x, int y]
        {
            get { return Map[x, y]; }
            set { Map[x, y] = value; }
        }
        public int xLen;
        public int yLen;
        public List<Water> bestWater;

        public WaterMap(WorldMap world, List<Point3d> source)
        {
            World = world;

            int sz_X = (int)(World.boundary.Width / World.resolution);
            int sz_Y = (int)(World.boundary.Height / World.resolution);
            double resX = World.boundary.Width / sz_X;
            double resY = World.boundary.Height / sz_Y;


            Points = new Point3d[sz_X, sz_Y];
            Map = new Water[sz_X, sz_Y];
            xLen = sz_X;
            yLen = sz_Y;
            //divide the rectangle into little rectangles, and assign location to be the midpoint of those rectangles.
            //set the locX, locY, value appropriately for MapArray 
            for (int y = 0; y < sz_Y; y++)
            {
                for (int x = 0; x < sz_X; x++)
                {
                    Points[x, y] = World.boundary.Plane.Origin +
                        (0.5 + x) * resX * World.boundary.Plane.XAxis +
                        (0.5 + y) * resY * World.boundary.Plane.YAxis;
                    Map[x, y] = new Water(this);
                    Map[x, y].locX = x;
                    Map[x, y].locY = y;
                    Map[x, y].val = 0;
                    Map[x, y].neighbours = new List<Water>();
                }
            }

            for (int x = 0; x < sz_X; x++)
            {
                for (int y = 0; y < sz_Y; y++)
                {
                    Map[x, y].GenerateNeighbours();
                }
            }

            foreach (Point3d point in source)
            {
                Tuple<int, int> coords = ClosestCoords(point);
                Map[coords.Item1, coords.Item2].val = 1000;
            }

            bestWater = Get_Best_Waterpoints();
        }

        public List<Water> Get_Best_Waterpoints()
        {
            List<Water> waters = new List<Water>();
            
            for (int i = 0; i < xLen; i++)
            {
                for (int j = 0; j < yLen; j++)
                {
                    waters.Add(Map[i, j]);
                }
            }

            waters = (from w in waters orderby w.val select w).Take(20).ToList();

            return waters;
        }

        public void UpdateWater()
        {

            for (int i = 0; i < xLen; i++)
            {
                for (int j = 0; j < yLen; j++)
                {
                    Water w = Map[i, j];
                    Terrain t = World.terrainMap[i, j];
                    double lowest_waterandterrainlevel = 0;
                    double LevelDifference = 0;

                    for (int k = 0; k < 10000; k++)
                    {
                        List<double> waterLevels = (from n in w.neighbours select n.val).ToList();
                        List<double> terrainLevels = (from n in t.neighbours select n.val).ToList();
                        List<double> waterAndTerrainLevels = new List<double>();
                        for (int l = 0; l < waterLevels.Count; l++)
                        {
                            if (double.IsInfinity(waterLevels[l])) continue;
                            double waterLevel = waterLevels[l];
                            double terrainLevel = terrainLevels[l];
                            waterAndTerrainLevels.Add(waterLevel + terrainLevel);
                        }

                        waterAndTerrainLevels.Sort();
                        lowest_waterandterrainlevel = waterAndTerrainLevels.First();

                        if (w.val + t.val <= lowest_waterandterrainlevel) break;

                        foreach (Water b in w.neighbours)
                        {
                            if (b.Map == null) continue;
                            int bx = b.locX;
                            int by = b.locY;
                            Terrain _tb = b.Map.World.terrainMap[bx, by];
                            if (b.val + _tb.val == lowest_waterandterrainlevel)
                            {
                                LevelDifference = (((w.val + t.val - (b.val + _tb.val)) / 2.0));
                                b.val += LevelDifference;
                                w.val -= LevelDifference;
                            }
                        }
                    }

                }
            }

            bestWater = Get_Best_Waterpoints();
        }

        public Tuple<int, int> ClosestCoords(Point3d point)
        {
            PointCloud pointCloud = new PointCloud();
            for (int y = 0; y < yLen; y++)
            {
                for (int x = 0; x < xLen; x++)
                {
                    pointCloud.Add(Points[x, y]);
                }
            }

            int i = pointCloud.ClosestPoint(point);
            return Tuple.Create(i % xLen, i / xLen);
        }

        public void Tick()
        {
            UpdateWater();
        }
    }

    public class Water
    {
        public int locX;
        public int locY;
        public double val;
        public WaterMap Map { get; private set; }
        public Water(WaterMap waterMap)
        {
            //?? => throw error if patchMap is null
            Map = waterMap ?? throw new ArgumentNullException("waterMap");
        }
        public Water()
        {
            //nothing
        }

        public List<Water> neighbours;

        public void GenerateNeighbours()
        {
            List<int> x_offsets = new List<int> { -1, -1, 0, 1, 1, 1, 0, -1 };
            List<int> y_offsets = new List<int> { 0, 1, 1, 1, 0, -1, -1, -1 };

            for (int i = 0; i < 8; i++)
            {
                int x = locX + x_offsets[i];
                int y = locY + y_offsets[i];

                if (x < 0 || x > Map.xLen - 1 ||
                    y < 0 || y > Map.yLen - 1)
                    neighbours.Add(Invalid);
                else
                    neighbours.Add(Map[x, y]);
            }
        }

        static public Water Invalid
        {
            get
            {
                return new Water
                {
                    locX = int.MaxValue,
                    locY = int.MaxValue,
                    val = double.PositiveInfinity
                };
            }
        }
    }

    public class PlantMap
    {
        public Plant[,] Map { get; }
        public Point3d[,] Points { get; }
        public WorldMap World { get; }
        public Plant this[int x, int y]
        {
            get { return Map[x, y]; }
            set { Map[x, y] = value; }
        }
        public int xLen;
        public int yLen;
        public List<Plant> desired_plants = new List<Plant>();
        

        public PlantMap(WorldMap _world, List<Point3d> source)
        {
            World = _world;

            int sz_X = (int)(World.boundary.Width / World.resolution);
            int sz_Y = (int)(World.boundary.Height / World.resolution);
            double resX = World.boundary.Width / sz_X;
            double resY = World.boundary.Height / sz_Y;

            
            Points = new Point3d[sz_X, sz_Y];
            Map = new Plant[sz_X, sz_Y];
            xLen = sz_X;
            yLen = sz_Y;
            //divide the rectangle into little rectangles, and assign location to be the midpoint of those rectangles.
            //set the locX, locY, value appropriately for MapArray 
            for (int y = 0; y < sz_Y; y++)
            {
                for (int x = 0; x < sz_X; x++)
                {
                    Points[x, y] = World.boundary.Plane.Origin +
                        (0.5 + x) * resX * World.boundary.Plane.XAxis +
                        (0.5 + y) * resY * World.boundary.Plane.YAxis;
                    Map[x, y] = new Plant(this);
                    Map[x, y].locX = x;
                    Map[x, y].locY = y;
                    Map[x, y].val = 0;
                    Map[x, y].neighbours = new List<Plant>();
                }
            }

            foreach (Point3d point in source)
            {
                Tuple<int, int> coords = ClosestCoords(point);
                Map[coords.Item1, coords.Item2].val = 1000;
            }

            //store neighbours
            for (int x = 0; x < sz_X; x++)
            {
                for (int y = 0; y < sz_Y; y++)
                {
                    Map[x, y].GenerateNeighbours();
                }
            }
        }

        public Tuple<int, int> ClosestCoords(Point3d point)
        {
            PointCloud pointCloud = new PointCloud();
            for (int y = 0; y < yLen; y++)
            {
                for (int x = 0; x < xLen; x++)
                {
                    pointCloud.Add(Points[x, y]);
                }
            }

            int i = pointCloud.ClosestPoint(point);
            return Tuple.Create(i % xLen, i / xLen);
        }

        public void UpdatePlant()
        {
            foreach (Plant plant in Map)
            {
                foreach (Plant_Turtle plant_indicator in World.plant_turtles)
                {
                    if (plant.locX == plant_indicator.location_x && plant.locY == plant_indicator.location_y)
                    {
                        plant.val += 100;
                        plant.step += 1;
                        if (plant.step >= 10)
                        {
                            desired_plants.Add(plant);
                        }
                    }
                }
            }
        }

        public void Tick()
        {
            UpdatePlant();
        }
    }
    public class Plant
    {
        public int locX;
        public int locY;
        public double val;
        public int step = 0;
        public PlantMap Map { get; private set; }
        public List<Plant> neighbours;

        public Plant()
        {
        }
        public Plant(PlantMap plantMap)
        {
            Map = plantMap ?? throw new ArgumentNullException("plantMap");
        }

        public void GenerateNeighbours()
        {
            List<int> x_offsets = new List<int> { -1, -1, 0, 1, 1, 1, 0, -1 };
            List<int> y_offsets = new List<int> { 0, 1, 1, 1, 0, -1, -1, -1 };

            for (int i = 0; i < 8; i++)
            {
                int x = locX + x_offsets[i];
                int y = locY + y_offsets[i];

                if (x < 0 || x > Map.xLen - 1 ||
                    y < 0 || y > Map.yLen - 1)
                    neighbours.Add(Invalid);
                else
                    neighbours.Add(Map[x, y]);
            }
        }

        static public Plant Invalid
        {
            get
            {
                return new Plant
                {
                    locX = int.MaxValue,
                    locY = int.MaxValue,
                    val = double.PositiveInfinity
                };
            }
        }
    }

    public class Plant_Turtle
    {
        WorldMap world;
        public int location_x;
        public int location_y;
        public double heading;
        public bool alive = true;
        public int target_x;
        public int target_y;

        public const double patchBias = 0.5;
        public const double visibility = 3.0; //turtle will move towards patch if it is this close (and other conditions met)
        public static Random r = new Random();

        public Plant_Turtle(int locX, int locY, double head, WorldMap _world)
        {
            location_x = locX;
            location_y = locY;
            heading = head;
            world = _world;
            alive = true;
            target_x = -1;
            target_y = -1;
        }

        public void MoveTowardsPatch(List<Plant> plants)
        {
            Point2d loc = new Point2d(location_x, location_y);
            Point2d target = new Point2d(target_x, target_y);

            List<Point2d> possiblePlants = new List<Point2d>();
            foreach(Plant p in plants)
            {
                Point2d plant = new Point2d(p.locX, p.locY);
                if(plant.DistanceTo(loc) < visibility)
                {
                    if(plant.DistanceTo(loc) < target.DistanceTo(plant))
                    {
                        possiblePlants.Add(new Point2d(p.locX, p.locY));
                    }
                }
            }

            if (possiblePlants.Count == 0)
                MoveTowardsTarget();
            else
            {
                Point2d chosenPlant = Point2d.Unset;
                if (possiblePlants.Count == 1) chosenPlant = possiblePlants[0];
                else
                {
                    chosenPlant = (from p in possiblePlants orderby p.DistanceTo(loc) select p).ToList()[0];
                }

                Vector2d direction = chosenPlant - loc;
                direction.Unitize();

                loc += direction;
                location_x = (int)Math.Round(loc.X);
                location_y = (int)Math.Round(loc.Y);

                ClampPositions();
            }
        }

        public void MoveTowardsTarget()
        {
            Point2d loc = new Point2d(location_x, location_y);
            Point2d target = new Point2d(target_x, target_y);
            Vector2d direction = target - loc;
            direction.Unitize();

            loc += direction;
            location_x = (int) Math.Round(loc.X);
            location_y = (int) Math.Round(loc.Y);

            ClampPositions();
        }

        public void ClampPositions()
        {
            int Xmax = world.plantMap.xLen - 1;
            int Ymax = world.plantMap.yLen - 1;

            location_x = location_x < 0 ? 0 : location_x > Xmax ? Xmax : location_x;
            location_y = location_y < 0 ? 0 : location_y > Ymax ? Ymax : location_y;
        }
        
        static public void FindNewTarget(List<Water> waters, out int tx, out int ty)
        {
            Water chosenOne = waters[r.Next(waters.Count)];
            tx = chosenOne.locX;
            ty = chosenOne.locY;
        }
        
    }
}
