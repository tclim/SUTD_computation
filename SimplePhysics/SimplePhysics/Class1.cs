using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace SimplePhysics
{
    public class Node
    {
        public Point3d position = Point3d.Origin;
        public Vector3d force = Vector3d.Zero;
        public Vector3d velocity = Vector3d.Zero;
        public double mass = 0.0;
        public double m0 = 0.0;
        public bool fix = false;

        public Node(Point3d point, double mass)
        {
           this.position = point;
           this.mass = mass;
        }

        public void ApplyForce(Vector3d forceToApply)
        {
            force += forceToApply;
        }

        public void Move(double dt, double damping)
        {
            if (fix) return;
            velocity *= damping;
            velocity += force * (dt / mass);
            position += velocity * dt;
            force = Vector3d.Zero;
        }
    }

    public class Edge
    {
        public Node n0;
        public Node n1;
        public double l0 = 0.0;
        public double k = 0.0;     //stiffness

        public Edge(Node node0, Node node1, double stiffness)
        {
            this.n0 = node0;
            this. n1 = node1;
            this.k = stiffness;
            this.l0 = n0.position.DistanceTo(n1.position);
            //n0.m += L0 * n0.m0 * 0.5 * 0.1 * 0.05;
            //n1.m += L0 * n0.m0 * 0.5 * 0.1 * 0.05
        }

        public void ApplySpringForce()
        {
            Vector3d dv = n1.position - n0.position;
            double length = dv.Length;
            dv.Unitize();
            n0.force += dv * (k * (length - l0)) * 0.5;
            n1.force -= dv * (k * (length - l0)) * 0.5;
        }
    }

    public class PhysicsSystem
    {
        public List<Node> nodes;
        public List<Edge> edges;
        public Vector3d gravity;

        public PhysicsSystem(Vector3d gravity)
        {
            nodes = new List<Node>();
            edges = new List<Edge>();
            this.gravity = gravity;
        }

        public void Reset()
        {
            nodes.Clear();
            edges.Clear();
        }

        public void Step(double dt, double damping)
        {
            // Apply Forces
            foreach (Node n in nodes) n.ApplyForce(gravity);
            foreach (Edge e in edges) e.ApplySpringForce();
            
            //Calculate 
            foreach (Node n in nodes) n.Move(dt, damping);
        }
    }
}