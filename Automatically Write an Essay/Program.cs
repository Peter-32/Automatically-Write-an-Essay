using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// This program will generate an essay on an inputted subject.  
// Pass the results into a grammar checking program to be sure there are no grammar mistakes.
namespace Automatically_Write_an_Essay
{
    class Program
    {
        static void Main(string[] args)
        {
            Controller.createEssay();
            Console.WriteLine("Essay completed.");
            // Console.WriteLine(Model.rand.Next(1));
            Console.ReadKey();
        }
    }

    public static class Model
    {
        // PROPERTIES
        public static string fileInput;
        public static int wordLimit;
        public static StringBuilder essay;
        public static string[] inputWords;

        static Model()
        {
            // DEFINE THESE TWO LINES WITH INPUT
            wordLimit = 500;
            fileInput = System.IO.File.ReadAllText(@"C:\Users\peter\OneDrive\Documents\Visual Studio 2015\Projects\input_output\pride_and_prejudice.txt");
            // The essay starts blank and the character limit is set to 20 times the word limit.
            essay = new StringBuilder("", wordLimit * 20);
            // split the input by white space
            inputWords = fileInput.Split(null);
        }

        // Dictionary: For a node (key), what edges are leaving it and going to another node? (Directed edges)
        public static Dictionary<Node, List<Edge>> dictionary = new Dictionary<Node, List<Edge>>();

        // Keeps the memory location of each node in a list
        public static List<Node> nodeList
        {
            get;
            set;
        } = new List<Node>();

        // Keeps the memory location of each edge in a list
        public static List<Edge> edgeList
        {
            get;
            set;
        } = new List<Edge>();

        public static Random rand = new Random();
    }

    public static class Controller
    {
        //// METHODS

        // CRUD Node
        public static void createNode(string key, int xLocation, int yLocation)
        {
            Node.Create(key, xLocation, yLocation);
        }

        public static Node selectNode(string searchKey)
        {
            foreach (var element in Model.nodeList)
            {
                if (element.key == searchKey)
                    return element;
            }
            Console.WriteLine("Tried to select a node but no such node exists");
            return null;
        }

        public static void modifyNode(Node node, string key, int xLocation, int yLocation)
        {
            // We don't just delete and recreate the node because then the edges
            // that depend on the node would be affected.

            // if this is a unique node key then there is no problem changing the values
            if (Controller.isUniqueNodeKey(key))
            {
                node.key = key;
                node.xLocation = xLocation;
                node.yLocation = yLocation;
            }
            // If it is not unique but has the same key value then we allow the user to update the location only
            else if (key == node.key)
            {
                node.xLocation = xLocation;
                node.yLocation = yLocation;
            }
            else
            {
                Console.WriteLine("Failed to modify node.  Not a unique key.");
                return;
            }
        }

        public static void deleteNode(Node node)
        {
            // remove all edges attached to the node
            foreach (Edge edge in Model.dictionary[node])
            {
                deleteEdge(edge);
            }
            // remove the entry from the dictionary
            Model.dictionary.Remove(node);
            // remove the node from the node list
            Model.nodeList.Remove(node);
        }

        // CRUD Edge
        public static void createEdge(string key, string description, Node fromNode, Node toNode)
        {
            Edge.Create(key, description, fromNode, toNode);
        }

        public static Edge selectEdge(string searchKey)
        {
            foreach (var element in Model.edgeList)
            {
                if (element.key == searchKey)
                    return element;
            }
            Console.WriteLine("Tried to select an edge but no such edge exists");
            return null;
        }

        public static void modifyEdge(Edge edge, string key, string description, Node fromNode, Node toNode)
        {
            // it is okay to delete the edge and recreate it here because
            // nodes aren't depending on the same edge instance existing.
            deleteEdge(edge);
            createEdge(key, description, fromNode, toNode);
        }

        public static void deleteEdge(Edge edge)
        {
            Model.dictionary[edge.fromNode].Remove(edge);
            Model.dictionary[edge.toNode].Remove(edge);
            Model.edgeList.Remove(edge);
        }

        // Validation methods
        public static bool isUniqueNodeKey(string checkKey)
        {
            int nodeListLength = Model.nodeList.Count();
            for (var i = 0; i < nodeListLength; i++)
            {
                if (checkKey == Model.nodeList[i].key)
                    return false;
            }
            return true;
        }
        public static bool isUniqueEdgeKey(string checkKey)
        {
            int edgeListLength = Model.edgeList.Count();
            for (var i = 0; i < edgeListLength; i++)
            {
                if (checkKey == Model.edgeList[i].key)
                    return false;
            }
            return true;
        }

        // Maybe this can be improved with a dictionary
        public static bool isNode(int x, int y)
        {
            foreach (var node in Model.nodeList)
            {
                if (node.xLocation == x && node.yLocation == y)
                    return true;
            }
            return false;
        }

        // Graph associations, what is attached to each other
        public static List<Edge> selectEdgesLeavingNode(Node queryNode)
        {
            return Model.dictionary[queryNode];
        }

        // Return an array of nodes that can be accessed from crossing one edge from a given node.
        public static List<Node> nextNodes(Node queryNode)
        {
            List<Node> nodes = new List<Node>();
            foreach (var edge in selectEdgesLeavingNode(queryNode))
            {
                nodes.Add(edge.toNode);
            }
            return nodes.Distinct().ToList(); // remove any duplication, then return the node list
        }

        // Next random node accessible from a given node.
        public static Node nextRandomNode(Node queryNode)
        {
            List<Node> nodes = nextNodes(queryNode);
            // call the function above in here
            if (nodes.Count() > 0)
            {
                int r = Model.rand.Next(nodes.Count());
                return nodes[r];
            }
            else
                return null;
        }

        public static void createNodesFromFile(string[] inputWords)
        {
            foreach (string word in inputWords)
            {
                // You could randomize the x, y locations.
                // this project doesn't have graphical output so it doesn't matter.
                createNode(word,0, 0);
            }
        }

        public static void createEdgesFromFile(string[] inputWords)
        {
            int numWordsInInput = inputWords.Count();
            for (var i = 1; i < numWordsInInput;  i++)
            {
                createEdge(String.Concat(inputWords[i - 1], " ", inputWords[i]), "", selectNode(inputWords[i - 1]), selectNode(inputWords[i]));
            }
        } 

        // Start at a random node.  Travel from node to node.  Build the essay until stuck or
        // the word limit of the essay is reached.  If stuck, recursively call this function.
        public static void buildEssayByRandomWalk(int wordsSoFar = 0)
        {
            // Choose a random arbitrary node
            int r = Model.rand.Next(Model.nodeList.Count());
            Node randomStartNode = Model.nodeList[r];


            // Add node to output file and use input file
            Model.essay.Append(randomStartNode.key);
            Model.essay.Append(" ");
            wordsSoFar++;

            Node nextNode = randomStartNode;
            while (true)
            {
                // get next node
                nextNode = nextRandomNode(nextNode);

                // if we can continue the loop continue to next node
                if (nextNode == null)
                    break;

                // Add node to output string
                Model.essay.Append(nextNode.key);
                Model.essay.Append(" ");
                wordsSoFar++;

                if (wordsSoFar > Model.wordLimit)
                    break;
            }

            // Branch based on null or word limit surpassed
            if (wordsSoFar > Model.wordLimit)
                return;
            else
                buildEssayByRandomWalk(wordsSoFar);
        }

        public static void createEssay()
        {
            //build nodes
            createNodesFromFile(Model.inputWords);

            //build edges
            createEdgesFromFile(Model.inputWords);

            // build the essay and store it in Model.essay
            buildEssayByRandomWalk();

            System.IO.File.WriteAllText(@"C:\Users\peter\OneDrive\Documents\Visual Studio 2015\Projects\input_output\essay.txt", Model.essay.ToString());
        }
    }

    public class Node
    {
        // CONSTRUCTORS
        private Node(string key, int xLocation, int yLocation)
        {
            this.key = key;
            this.xLocation = xLocation;
            this.yLocation = yLocation;
        }

        public static void Create(string key, int xLocation, int yLocation)
        {
            if (Controller.isUniqueNodeKey(key))
            {
                Node newNode = new Node(key, xLocation, yLocation);
                // The graphing controller needs to update the node list
                Model.nodeList.Add(newNode);
                // The graphing controller needs to update its dictionary
                Model.dictionary.Add(newNode, new List<Edge>());
                return;
            }
            else
            {
                //Console.WriteLine("Failed to create node.  Not a unique key.");
                return;
            }
        }
        // PROPERTIES
        public string key
        {
            get;
            set;
        }

        public int xLocation
        {
            get;
            set;
        }

        public int yLocation
        {
            get;
            set;
        }

        // METHODS
        public override string ToString()
        {
            return String.Concat("Node ", key, " - At location (", xLocation, ",", yLocation, ")");
        }
    }

    // All edges are directed edges
    public class Edge
    {
        // CONSTRUCTORS
        private Edge(string key, string description, Node fromNode, Node toNode)
        {
            this.description = description;
            this.key = key;
            this.fromNode = fromNode;
            this.toNode = toNode;
        }

        public static void Create(string key, string description, Node fromNode, Node toNode)
        {
            if (Controller.isUniqueEdgeKey(key))
            {
                Edge newEdge = new Edge(key, description, fromNode, toNode);
                // The graphing controller needs to update the edge list
                Model.edgeList.Add(newEdge);
                // The graphing controller needs to update its dictionary with the fromNode.
                // This way we know the edges leaving the fromNode
                Model.dictionary[fromNode].Add(newEdge);

                return;
            }
            else
            {
                //Console.WriteLine("Failed to create edge.  Not a unique key.");
                return;
            }
        }

        // PROPERTIES
        public string description
        {
            get;
            set;
        }
        public string key
        {
            get;
            set;
        }
        public Node fromNode
        {
            get;
            set;
        }
        public Node toNode
        {
            get;
            set;
        }

        // METHODS
        public override string ToString()
        {
            return String.Concat("Edge ", key, " - ", description);
        }

    }
}