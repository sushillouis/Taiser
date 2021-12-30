using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine;

public partial class PacketRule : List<PacketRule.Details> {
	// Enum defining a packet's color
	[Serializable]
	public enum Color : byte {
		Blue,
		Pink,
		Green,
		Any	// Only used by the parser
	}

	// Enum defining a packet's size
	[Serializable]
	public enum Size : byte {
		Invalid = 0,
		Small = 1,
		Medium = 4,
		Large = 6,
		Any	// Only used by the parser
	}

	// Enum defining a packet's shape
	[Serializable]
	public enum Shape : byte {
		Cube,
		Sphere,
		Cone,
		Any	// Only used by the parser
	}

	// Structure which stores the details (color, size, and shape) of a packet
	[Serializable]
	public struct Details {
		public Color color;
		public Size size;
		public Shape shape;

		public static readonly Details Default = new Details(Color.Blue, Size.Small, Shape.Cube);
		public static readonly Details Any = new Details(Color.Any, Size.Any, Shape.Any);

		public Details(Color _color, Size _size, Shape _shape){
			color = _color;
			size = _size == Size.Invalid ? Size.Small : _size;
			shape = _shape;
		}

		// Object equality (Required to override ==)
		public override bool Equals(System.Object obj) {
			if (obj is null)
				return false;
			Details? o = obj as Details?;
			return Equals(o.Value);
		}

		// Details equality
		public bool Equals(Details o){
			return color == o.color
				&& size == o.size
				&& shape == o.shape;
		}

		// Required to override Equals
		public override int GetHashCode() { return base.GetHashCode(); }

		// Equality Operator
		public static bool operator ==(Details a, Details b){ return a.Equals(b); }
		// Inequality Operator (Required if == is overridden)
		public static bool operator !=(Details a, Details b){ return !a.Equals(b); }

		// To string method used for selection debugging
		public override string ToString(){
			return "Color: " + color + ", Size: " + size + ", Shape: " + shape;
		}
	}


	// -- Parse Nodes --


	// Base Parse Node
	public class Node {
		public enum Type {
			Or,
			And,
			Not,
			Literal
		}
		// Tracking the type of the node
		public Type type;
		// Parent of this node
		public Node parent;
		// List of child nodes
		public Node[] children;

		public Node() {
			parent = null;
			children = null;
		}

		// Base function which returns a rule string for the whole tree
		public virtual string RuleString(){
			return "";
		}

		// Function used to dump tree nodes
		public virtual void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- emptyNode");
		    indent += last ? "   " : "|  ";

			for(int i = 0; i < children.Length; i++)
				children[i].DebugDump(indent, i == children.Length - 1);
		}

		// Function which checks if all of the provided nodes have the same type
		public static bool allHaveType(Node[] nodes, Node.Type type){
			foreach(Node node in nodes)
				if(node.type != type)
					return false;
			return true;
		}

		// Function which checks if all of the provided nodes have the same type in a list of types
		public static bool allHaveTypes(Node[] nodes, Node.Type[] types){
			bool has = false;
			foreach(Node node in nodes)
				foreach(Node.Type type in types)
					has |= (node.type == type);

			return has;
		}
	}

	// Or Node
	public class ORNode : Node {
		// Subnodes
		public static readonly uint left = 0;
		public static readonly uint right = 1;

		public ref Node getLeft() => ref children[left];
		public ref Node getRight() => ref children[right];

		public ORNode() {
			type = Node.Type.Or;
			children = new Node[2];
		}
		public ORNode(Node _left, Node _right) : this() {
			children[left] = _left;
			children[right] = _right;
		}

		// Recursively generate the OR node's string
		public override string RuleString(){
			string ret = "";

			if(children is object){
				if(children[0].children is object && children[0].children.Length > 1)
					ret = "(" + children[0].RuleString() + ")";
				else
					ret = children[0].RuleString();

				for(int i = 1; i < children.Length; i++)
					if(children[i].children is object && children[i].children.Length > 1)
						ret += " | (" + children[i].RuleString() + ")";
					else
						ret += " | " + children[i].RuleString();
			}

			return ret;
		}

		// Function used to dump tree nodes
		public override void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- OR");
		    indent += last ? "   " : "|  ";

			for(int i = 0; i < children.Length; i++)
				children[i].DebugDump(indent, i == children.Length - 1);
		}
	}

	// And Nodes
	public class ANDNode: Node {
		// Subnodes
		public static readonly uint left = 0;
		public static readonly uint right = 1;

		public ref Node getLeft() => ref children[left];
		public ref Node getRight() => ref children[right];

		public ANDNode() {
			type = Node.Type.And;
			children = new Node[2];
		}
		public ANDNode(Node _left, Node _right) : this() {
			children[left] = _left;
			children[right] = _right;
		}

		// Recursively generate the AND node's string
		public override string RuleString(){
			string ret = "";

			if(children is object){
				if(children[0].children is object && children[0].children.Length > 1)
					ret = "(" + children[0].RuleString() + ")";
				else
					ret = children[0].RuleString();

				for(int i = 1; i < children.Length; i++)
					if(children[i].children is object && children[i].children.Length > 1)
						ret += " & (" + children[i].RuleString() + ")";
					else
						ret += " & " + children[i].RuleString();
			}

			return ret;
		}

		// Function used to dump tree nodes
		public override void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- AND");
		    indent += last ? "   " : "|  ";

			for(int i = 0; i < children.Length; i++)
				children[i].DebugDump(indent, i == children.Length - 1);
		}
	}

	// Not Nodes
	public class NotNode : Node {
		// Subnodes
		public static readonly uint child = 0;

		public ref Node getChild() => ref children[child];

		public NotNode() {
			type = Node.Type.Not;
			children = new Node[1];
		}
		public NotNode(Node child) : this() {
			children[0] = child;
		}

		// Recursively generate the NOT node's string
		public override string RuleString(){
			string ret = "";

			if(children is object){
				if(children[0].children is object && children[0].children.Length > 1)
					ret = " !(" + children[0].RuleString() + ")";
				else
					ret = " !" + children[0].RuleString();
			}

			return ret;
		}

		// Function used to dump tree nodes
		public override void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- NOT");
		    indent += last ? "   " : " |  ";

			for(int i = 0; i < children.Length; i++)
				children[i].DebugDump(indent, i == children.Length - 1);
		}
	}

	// Literal Nodes
	public class LiteralNode : Node {
		// Details of the literal
		public Details details;

		public LiteralNode() { type = Node.Type.Literal; }
		public LiteralNode(Details d) {
			type = Node.Type.Literal;
			details = d;
		}

		// Constants providing easy access to all of the nodes holding a specific individual rule
		public static readonly LiteralNode Pink = new LiteralNode(new Details(Color.Pink, Size.Any, Shape.Any));
		public static readonly LiteralNode Blue = new LiteralNode(new Details(Color.Blue, Size.Any, Shape.Any));
		public static readonly LiteralNode Green = new LiteralNode(new Details(Color.Green, Size.Any, Shape.Any));
		public static readonly LiteralNode Small = new LiteralNode(new Details(Color.Any, Size.Small, Shape.Any));
		public static readonly LiteralNode Medium = new LiteralNode(new Details(Color.Any, Size.Medium, Shape.Any));
		public static readonly LiteralNode Large = new LiteralNode(new Details(Color.Any, Size.Large, Shape.Any));
		public static readonly LiteralNode Sphere = new LiteralNode(new Details(Color.Any, Size.Any, Shape.Sphere));
		public static readonly LiteralNode Cone = new LiteralNode(new Details(Color.Any, Size.Any, Shape.Cone));
		public static readonly LiteralNode Cube = new LiteralNode(new Details(Color.Any, Size.Any, Shape.Cube));

		// Generate the Literal nodes's string
		public override string RuleString(){
			string ret = "";

			if(details.color != Color.Any)
				ret += System.Enum.GetName(typeof(PacketRule.Color), details.color);

			if(details.size != Size.Any){
				if(details.color != Color.Any)
					ret += " & ";
				ret += System.Enum.GetName(typeof(PacketRule.Size), details.size);
			}

			if(details.shape != Shape.Any){
				if(details.color != Color.Any || details.size != Size.Any)
					ret += " & ";
				ret += System.Enum.GetName(typeof(PacketRule.Shape), details.shape);
			}


			if(ret.Contains("&"))
				ret = "(" + ret + ")";

			return ret;
		}

		// Function used to dump tree nodes
		public override void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- " + details);
		    indent += last ? "   " : "|  ";
		}

		// Create a list of Literal Nodes from a list of Details
		public static LiteralNode[] fromDetails(IEnumerable<Details> input){
			List<LiteralNode> ret = new List<LiteralNode>();
			foreach(Details d in input){
				LiteralNode node = new LiteralNode();
				node.details = d;
				ret.Add(node);
			}

			return ret.ToArray();
		}

		// Create a list of Details from a list of Literal Nodes
		public static Details[] detailsFromNodes(IEnumerable<LiteralNode> input){
			List<Details> ret = new List<Details>();
			foreach(LiteralNode node in input){
				Details d = new Details();
				d = node.details;
				ret.Add(d);
			}

			return ret.ToArray();
		}
	}


	// -- Class Storage --


	// Parse node representing the unoptimized tree;
	public Node treeRoot;
	// Constant representing a rule with every possible packet in it
	public static readonly PacketRule All = Parse("pink | blue | green | small | medium | large | sphere | cone | cube");
	public static readonly PacketRule Default = Parse("Blue & Cube & Small");

	// Override clear to also take out the treeRoot
	public new void Clear() {
		base.Clear();
		treeRoot = null;
	}

	// Converts the rule into a string representation (if that string representation is parsed it results in the same rule)
	public string RuleString() => treeRoot?.RuleString() ?? "";
	// Converts the rule into its lexed form (if that representation is parsed it results in the same rule)
	public string CompressedRuleString() => Lex(RuleString());
	// Override of ToString which simply returns the rule string
	public override string ToString() => RuleString();

	// Function which merges two PacketRules together (Ensuring unique elements and properly optimized trees)
	public PacketRule Union_InPlace(string otherString) {
		string combined = "";
		if(!this.EmptyRule()) combined += "(" + RuleString() + ")";
		if(!EmptyRule(otherString)) combined += (combined.Length > 0 ? " | " : "") + "(" + otherString + ")";
		if(combined.Length > 0) Parse(combined);
		return this;
	}
	public PacketRule Union_InPlace(PacketRule other) => Union_InPlace(other.RuleString());
	public PacketRule Union(PacketRule other) { PacketRule _out = this.MemberwiseClone() as PacketRule; return _out.Union_InPlace(other.RuleString()); }
	public PacketRule Union(string other) { PacketRule _out = this.MemberwiseClone() as PacketRule; return _out.Union_InPlace(other); }
	// Function which finds the common elements of two PacketRules (Ensuring unique elements and properly optimized trees)
	public PacketRule Intersection_InPlace(string otherString) {
		if(this.EmptyRule()) return this;
		if(EmptyRule(otherString)) { Clear(); return this; }

		Parse("(" + RuleString() + ") & (" + otherString + ")");
		return this;
	}
	public PacketRule Intersection_InPlace(PacketRule other) => Intersection_InPlace(other.RuleString());
	public PacketRule Intersection(PacketRule other) { PacketRule _out = this.MemberwiseClone() as PacketRule; return _out.Intersection_InPlace(other.RuleString()); }
	public PacketRule Intersection(string other) { PacketRule _out = this.MemberwiseClone() as PacketRule; return _out.Intersection_InPlace(other); }
	// Function which removes a PacketRule from this rule (Ensuring unique elements and properly optimized trees)
	public PacketRule Difference_InPlace(string otherString){
		if(this.EmptyRule()) return this;
		if(EmptyRule(otherString)) return this;
		
		Parse("(" + RuleString() + ") & !(" + otherString + ")");
		return this;
	}
	public PacketRule Difference_InPlace(PacketRule other) => Difference_InPlace(other.RuleString());
	public PacketRule Difference(PacketRule other) { PacketRule _out = this.MemberwiseClone() as PacketRule; return _out.Difference_InPlace(other.RuleString()); }
	public PacketRule Difference(string other) { PacketRule _out = this.MemberwiseClone() as PacketRule; return _out.Difference_InPlace(other); }

	// Function which checks if this rulke is empty
	private static bool EmptyRule(string rule) {
		if(rule.Length == 0) return true;
		return rule == "()" || rule == "()|()";
	}
	private static bool EmptyRule(PacketRule rule) => EmptyRule(rule.RuleString());
	public bool EmptyRule() => EmptyRule(this);


	// -- Parser --


	// Parses a string into a packet rule
	// Includes an option to automatically optimize the parsed result and create the list out of it
	public static PacketRule Parse(string s, bool autoCommit = true){
		// // Debugging
		// Debug.Log("Input: " + s);

		// Lex the input string down to single characters
		s = Lex(s);

		// // Debugging
		// Debug.Log("Lexed: " + s);

		// Parse the lexed tokens into a tree
		int index = 0;
		PacketRule ret = new PacketRule();
		ret.treeRoot = ParseExpression(s, ref index);

		// Apply optimizations and list conversion to the tree if autoCommitting is requested
		if(autoCommit)
			ret.Commit();

		return ret;
	}

	// Sizes: b = blue, p = pink, g = green
	// Shapes: s = sphere, c = cone, r = rectangle
	// Sizes: t = small, m = medium, l = large
	// Operations: () = parenthesis, & = and, | = or, ! = not
 	static string Lex(string s){
		// Add a space to the end of the input to ensure that the last token is found
		s = s + " ";

		// Color
		s = Regex.Replace(s, @"(blue|b)([ &|\(\)])", "b$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(pink|red|p)([ &|\(\)])", "p$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(green|g)([ &|\(\)])", "g$+", RegexOptions.IgnoreCase);

		// Shape
		s = Regex.Replace(s, @"(circle|sphere|cir|sp|s)([ &|\(\)])", "s$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(cone|triangle|co|tri|c)([ &|\(\)])", "c$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(square|rectangle|cube|box|rect|sq|r)([ &|\(\)])", "r$+", RegexOptions.IgnoreCase);

		// Size
		s = Regex.Replace(s, @"(small|sm|t)([ &|\(\)])", "t$+", RegexOptions.IgnoreCase); // S already taken so using the next letter (t)
		s = Regex.Replace(s, @"(medium|med|m)([ &|\(\)])", "m$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(large|lg|l)([ &|\(\)])", "l$+", RegexOptions.IgnoreCase);

		// Operation
		s = Regex.Replace(s, "(and|conjunction|&&|&)", "&", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(or|disjunction|\|\||\|)", "|", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, "(not|!)", "!", RegexOptions.IgnoreCase);

		// Remove spaces
		s = s.Replace(" ", "");

		// Ensure that only valid tokens remain in the string
		char[] validTokens = new char[] {'b', 'p', 'g', 's', 'c', 'r', 't', 'm', 'l', '&', '|', '!', '(', ')'}; // List of valid tokens
		for(int i = 0; i < s.Length; i++)
			if(!validTokens.Contains(s[i]))
				throw new System.ArgumentException("Found unsupported toxen: `" + s[i] + "`");

		return s;
	}

	// S -> Expression
	// Expression -> Subexpression OR Expression
	//				| Subexpression
	// Subexpression -> NOT Subexpression
	//				| ExtendedLiteral AND Subexpression
	//				| ExtendedLiteral
	// ExtendedLiteral -> (Expression)
	//				| Literal


	// Parse an expression
	// Expression -> Subexpression OR Expression
	//				| Subexpression
	static Node ParseExpression(string tokens, ref int index){
		// Return node
		Node ret = null;
		// Left hand operand (needs to be parsed first since appears first in grammar)
		Node left = ParseSubexpression(tokens, ref index);

		// If there is an OR, then create an OR node (also parsing the right half)
		if(index < tokens.Length && tokens[index] == '|'){
			index++;
			ret = new ORNode();
			(ret as ORNode).getLeft() = left;
			(ret as ORNode).getRight() = ParseExpression(tokens, ref index);
			// Mark the output node as the parent to the parsed sub-nodes
			(ret as ORNode).getLeft().parent = ret;
			(ret as ORNode).getRight().parent = ret;
		// Otherwise return the subexpression we paresed
		} else
			ret = left;

		return ret;
	}

	// Parse a Subexpression
	// Subexpression -> NOT Subexpression
	//				| ExtendedLiteral AND Subexpression
	//				| ExtendedLiteral
	static Node ParseSubexpression(string tokens, ref int index){
		// The return node
		Node ret = null;

		// If there is a not, create a not node parse its child and return
		if(tokens[index] == '!'){
			index++;
			ret = new NotNode();
			(ret as NotNode).getChild() = ParseSubexpression(tokens, ref index);
			(ret as NotNode).getChild().parent = ret;
			return ret;
		}

		// Parse the left hand operand (needs to be parsed first since appears first in grammar)
		Node left = ParseExtendedLiteral(tokens, ref index);

		// If there is an and, create an and node (parsing the right half)
		if(index < tokens.Length && tokens[index] == '&'){
			index++;
			ret = new ANDNode();
			(ret as ANDNode).getLeft() = left;
			(ret as ANDNode).getRight() = ParseSubexpression(tokens, ref index);
			// Mark the output node as the parent to the parsed sub-nodes
			(ret as ANDNode).getLeft().parent = ret;
			(ret as ANDNode).getRight().parent = ret;
		// Otherwise, return the parsed extended literal
		} else
			ret = left;

		return ret;
	}

	// Parse an ExtendedLiteral
	// ExtendedLiteral -> (Expression)
	//				| Literal
	static Node ParseExtendedLiteral(string tokens, ref int index){
		// Return node
		Node ret = null;

		// If there is a parenthesis, parse its child
		if(tokens[index] == '('){
			index++;
			ret = ParseExpression(tokens, ref index);

			// Make sure that an open parenthesis has a matching close parenthesis
			if(tokens[index] != ')')
				throw new System.ArgumentException("Found expected `)` index: " + index + " in `" + tokens + "`");
			index++;
		// Otherwise, parse the literal
		} else
			ret = ParseLiteral(tokens, ref index);

		return ret;
	}

	// Parse a literal, converting it from a string to a set of packet details with a multitude of anys
	static Node ParseLiteral(string tokens, ref int index){
		string literal = "";
		literal += tokens[index];
		index++;

		LiteralNode ret = new LiteralNode();
		ret.details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Any, PacketRule.Shape.Any);

		foreach(char c in literal){
			switch(c){
				case 'b': // Blue
					ret.details.color = PacketRule.Color.Blue;
					break;
				case 'p':  // Pink
					ret.details.color = PacketRule.Color.Pink;
					break;
				case 'g':  // Green
					ret.details.color = PacketRule.Color.Green;
					break;
				case 's':  // Sphere
					ret.details.shape = PacketRule.Shape.Sphere;
					break;
				case 'c':  // Cone
					ret.details.shape = PacketRule.Shape.Cone;
					break;
				case 'r':  // Cube
					ret.details.shape = PacketRule.Shape.Cube;
					break;
				case 't':  // Small
					ret.details.size = PacketRule.Size.Small;
					break;
				case 'm':  // Medium
					ret.details.size = PacketRule.Size.Medium;
					break;
				case 'l':  // Large
					ret.details.size = PacketRule.Size.Large;
					break;
				default:
					throw new System.ArgumentException("Found invalid literal containing " + c + " at index: " + index + " in `" + tokens + "`");
			}
		}

		return ret;
	}


	// -- Optimizer


	// Function which commits the changes to the parse tree
	// Converts the parse tree into a fully expanded sum of products form
	public void Commit() {
		// Make sure that we are no longer storing any values (calling base version so we don't take out the parsed tree)
		base.Clear();

		// Make a deep copy of the tree so that we can manipulate it without worying about the integrity of the original
		Node treeCopy = ObjectExtensions.Copy(treeRoot); // Code found in Utilities/ObjectExtensions.cs

		// If the tree is just a single literal then expand that literal
		if(treeCopy.type == Node.Type.Literal){
			ORNode ret = new ORNode();
			ret.children = LiteralNode.fromDetails(expandSingleLiteralNode(treeCopy as LiteralNode));
			treeCopy = ret;

		// Otherwise manipulate the tree into its final form
		} else {
			// Remove NOTs from the copied tree
			treeCopy = optimizeAllNots(treeCopy);

			// Try to combine literals (if there are a bunch of literals anded together then they should be easy to expand)
			treeCopy = combineAndExpandAllLiterals(treeCopy);

			// Distrubte ANDs over ORs (move ANDs to the bottom of the tree)
			treeCopy = distributeAllAnd(treeCopy);

			// Repeatedly consildate the tree then expand literals until the tree is a single OR node with Literal children
			bool bothChanged = true;
			while(bothChanged){
				bothChanged = false;

				// Consolidate until no changes occurred
				bool changed = true;
				while(changed){
					changed = false;
					treeCopy = consolidateAll(treeCopy, ref changed);
					bothChanged |= changed;
				}

				// Combine and Expand until no changes occurred
				changed = true;
				while(changed){
					changed = false;
					treeCopy = combineAndExpandAllLiterals(treeCopy, ref changed);
					bothChanged |= changed;
				}

				// If the tree is just a single literal then expand that literal and we are done
				if(treeCopy.type == Node.Type.Literal){
					ORNode ret = new ORNode();
					ret.children = LiteralNode.fromDetails(expandSingleLiteralNode(treeCopy as LiteralNode));
					treeCopy = ret;

					bothChanged = false;
				}
			}
		}

		// // Debugging
		// Debug.Log("Optimized:");
		// treeCopy.DebugDump();

		// Update ourselves to be the list of children of the single OR node left remaining in the parse tree
		AddRange( LiteralNode.detailsFromNodes(treeCopy.children as LiteralNode[]) );
	}

	// Function which ensures that all nots are removed from the tree
	Node optimizeAllNots(Node node){
		// If a node is a NOT, optimize it and then recursively call on the result
		if(node.type == Node.Type.Not){
			return optimizeAllNots( optimizeNot(node as NotNode) );
		// If the node is an AND, recursively call on its children
		} else if (node.type == Node.Type.And){
			ANDNode andNode = node as ANDNode;
			andNode.getLeft() = optimizeAllNots(andNode.getLeft());
			andNode.getRight() = optimizeAllNots(andNode.getRight());
			return andNode;
		// If the node is an OR, recursively call on its children
		} else if (node.type == Node.Type.Or){
		   ORNode orNode = node as ORNode;
		   orNode.getLeft() = optimizeAllNots(orNode.getLeft());
		   orNode.getRight() = optimizeAllNots(orNode.getRight());
		   return orNode;
	   }

	   return node;
	}

	// Function which optimizes away nots
	Node optimizeNot(NotNode input){
		if(input.getChild().type == Node.Type.Literal){
			LiteralNode child = input.getChild() as LiteralNode;

			// Single literal rule

			// Color
			if(child.details.color != PacketRule.Color.Any && child.details.shape == PacketRule.Shape.Any && child.details.size == PacketRule.Size.Any){
				ORNode ret = new ORNode();

				switch(child.details.color){
					case PacketRule.Color.Blue:
						ret.getLeft() = new LiteralNode();
						(ret.getLeft() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Pink, PacketRule.Size.Any, PacketRule.Shape.Any);
						ret.getRight() = new LiteralNode();
						(ret.getRight() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Green, PacketRule.Size.Any, PacketRule.Shape.Any);
						break;
					case PacketRule.Color.Pink:
						ret.getLeft() = new LiteralNode();
						(ret.getLeft() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Blue, PacketRule.Size.Any, PacketRule.Shape.Any);
						ret.getRight() = new LiteralNode();
						(ret.getRight() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Green, PacketRule.Size.Any, PacketRule.Shape.Any);
						break;
					case PacketRule.Color.Green:
						ret.getLeft() = new LiteralNode();
						(ret.getLeft() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Pink, PacketRule.Size.Any, PacketRule.Shape.Any);
						ret.getRight() = new LiteralNode();
						(ret.getRight() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Blue, PacketRule.Size.Any, PacketRule.Shape.Any);
						break;
				}

				return ret;
			}

			// Shape
			if (child.details.shape != PacketRule.Shape.Any && child.details.color == PacketRule.Color.Any && child.details.size == PacketRule.Size.Any){
				ORNode ret = new ORNode();

				switch(child.details.shape){
					case PacketRule.Shape.Sphere:
						ret.getLeft() = new LiteralNode();
						(ret.getLeft() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Any, PacketRule.Shape.Cube);
						ret.getRight() = new LiteralNode();
						(ret.getRight() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Any, PacketRule.Shape.Cone);
						break;
					case PacketRule.Shape.Cone:
						ret.getLeft() = new LiteralNode();
						(ret.getLeft() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Any, PacketRule.Shape.Sphere);
						ret.getRight() = new LiteralNode();
						(ret.getRight() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Any, PacketRule.Shape.Cube);
						break;
					case PacketRule.Shape.Cube:
						ret.getLeft() = new LiteralNode();
						(ret.getLeft() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Any, PacketRule.Shape.Sphere);
						ret.getRight() = new LiteralNode();
						(ret.getRight() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Any, PacketRule.Shape.Cone);
						break;
				}

				return ret;
			}

			// Size
			if (child.details.size != PacketRule.Size.Any && child.details.shape == PacketRule.Shape.Any && child.details.color == PacketRule.Color.Any){
				ORNode ret = new ORNode();

				switch(child.details.size){
					case PacketRule.Size.Small:
						ret.getLeft() = new LiteralNode();
						(ret.getLeft() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Medium, PacketRule.Shape.Any);
						ret.getRight() = new LiteralNode();
						(ret.getRight() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Large, PacketRule.Shape.Any);
						break;
					case PacketRule.Size.Medium:
						ret.getLeft() = new LiteralNode();
						(ret.getLeft() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Small, PacketRule.Shape.Any);
						ret.getRight() = new LiteralNode();
						(ret.getRight() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Large, PacketRule.Shape.Any);
						break;
					case PacketRule.Size.Large:
						ret.getLeft() = new LiteralNode();
						(ret.getLeft() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Small, PacketRule.Shape.Any);
						ret.getRight() = new LiteralNode();
						(ret.getRight() as LiteralNode).details = new PacketRule.Details(PacketRule.Color.Any, PacketRule.Size.Medium, PacketRule.Shape.Any);
						break;
				}

				return ret;
			}

			// TODO: Possibly... if needed rules could be added for generating additional not cases
			throw new System.ArgumentException("Not Optimization of multi-rule literals not supported");
		}

		// Apply De'Morgans on child AND nodes
		else if(input.getChild().type == Node.Type.And){
			ANDNode child = input.getChild() as ANDNode;
			ORNode ret = new ORNode();

			NotNode left = new NotNode();
			left.getChild() = child.getLeft();
			// Recusrively optimize the not
			ret.getLeft() = optimizeNot(left);

			NotNode right = new NotNode();
			right.getChild() = child.getRight();
			// Recursively optimize the not
			ret.getRight() = optimizeNot(right);

			return ret;
		}

		// Apply De'Morgans on child OR nodes
		else if(input.getChild().type == Node.Type.Or){
			ORNode child = input.getChild() as ORNode;
			ANDNode ret = new ANDNode();

			NotNode left = new NotNode();
			left.getChild() = child.getLeft();
			// Recusrively optimize the not
			ret.getLeft() = optimizeNot(left);

			NotNode right = new NotNode();
			right.getChild() = child.getRight();
			// Recusrively optimize the not
			ret.getRight() = optimizeNot(right);

			return ret;
		}

		// If there is a double not, simply remove them
		else if(input.getChild().type == Node.Type.Not)
			return (input.getChild() as NotNode).getChild();

		throw new System.ArgumentException("Unsupported Not-Optimiztion case!");
	}



	// Function which repeatedly distributes until nothing is changed
	Node distributeAllAnd(Node node) {
		// While something has changed...
		bool changed = true;
		while(changed){
			changed = false;
			// Take another pass at distributing ANDs
			node = distributeAllAnd(node, ref changed);
		}

		return node;
	}

	// Function which recursively distributes ANDs over ORs in the whole tree
	Node distributeAllAnd(Node node, ref bool changed){
		// If this node is an and node... distribute over it
		if(node.type == Node.Type.And)
			node = distributeAnd(node as ANDNode, ref changed);

		// Recursively call for each child
		if(node.children is object)
			for(int i = 0; i < node.children.Length; i++)
				node.children[i] = distributeAllAnd(node.children[i], ref changed);

		return node;
	}

	// Function which distributes an AND over an OR (tracks if a change occurred)
	Node distributeAnd(ANDNode input, ref bool changed){
		// If the left node of the AND is an OR, distribute
		if(input.getLeft().type == Node.Type.Or){
			ORNode child = input.getLeft() as ORNode;
			// Create a new OR node
			ORNode ret = new ORNode();

			// Create an AND node on the left (it has the OR's left and the original right)
			ret.getLeft() = new ANDNode();
			(ret.getLeft() as ANDNode).getLeft() = child.getLeft();
			(ret.getLeft() as ANDNode).getRight() = ObjectExtensions.Copy(input.getRight());

			// Create an AND node on the right (it has the OR's right and the original right)
			ret.getRight() = new ANDNode();
			(ret.getRight() as ANDNode).getLeft() = child.getRight();
			(ret.getRight() as ANDNode).getRight() = ObjectExtensions.Copy(input.getRight());

			changed |= true; // Mark that a change occurred
			return ret;
		// If the right node of the AND is an OR, distribute
		} else if(input.getRight().type == Node.Type.Or){
			ORNode child = input.getRight() as ORNode;
			// Create a new OR node
			ORNode ret = new ORNode();

			// Create an AND node on the left (it has the original left and the OR's left)
			ret.getLeft() = new ANDNode();
			(ret.getLeft() as ANDNode).getLeft() = ObjectExtensions.Copy(input.getLeft());
			(ret.getLeft() as ANDNode).getRight() = child.getLeft();

			// Create an AND node on the right (it has the original left and the OR's right)
			ret.getRight() = new ANDNode();
			(ret.getRight() as ANDNode).getLeft() = ObjectExtensions.Copy(input.getLeft());
			(ret.getRight() as ANDNode).getRight() = child.getRight();

			changed |= true; // Mark that a change occurred
			return ret;
		}

		return input;
	}



	// Function which repeatedly consolidates until nothing is changed
	Node consolidateAll(Node node){
		// While something has changed, consolidate the whole tree
		bool changed = true;
		while(changed){
			changed = false;
			node = consolidateAll(node, ref changed);
		}

		return node;
	}

	// Function which recursively consolidates nodes in the tree
	Node consolidateAll(Node node, ref bool changed){
		// Consolidate this node
		node = consolidate(node, ref changed);
		// Recursively consolidate the children
		if(node.children is object)
			 for(int i = 0; i < node.children.Length; i++)
				node.children[i] = consolidateAll(node.children[i], ref changed);

		return node;
	}

	// Function which consolidates a single node (removed duplicate literals, combines A AND A and A OR A into A, and merges chains of AND/ORs into a single node)
	Node consolidate(Node node, ref bool changed){
		// Returned node
		Node ret = node;

		// If this node is an OR node...
		if(node.type == Node.Type.Or){
			ORNode orNode = node as ORNode;

			// If all of the children are literals... remove any children of this node that are the same
			if(Node.allHaveType(orNode.children, Node.Type.Literal) && orNode.children is object){
				List<Node> children = new List<Node>(orNode.children);

				// Remove duplicates
				for(int i = 0; i < children.Count; i++)
					for(int j = i + 1; j < children.Count; j++)
						if((children[i] as LiteralNode).details == (children[j] as LiteralNode).details)
							children.RemoveAt(j);

				// If only a single child is remaining, this node just becomes the child
				if(children.Count == 1)
					ret = children[0];
				// Otherwise update the list of children
				else {
					orNode.children = children.ToArray();
					ret = orNode;
				}

				// If the number of children changed... mark that a change has occurred
				if(children.Count != orNode.children.Length)
					changed |= true;

			// If all of the children are OR nodes
			} else if(Node.allHaveType(orNode.children, Node.Type.Or) && orNode.children is object){
				// Create a list of all of the children's children
				List<Node> children = new List<Node>();
				foreach(Node child in orNode.children)
					foreach(Node childsChild in child.children)
						children.Add(childsChild);

				// Our children become the children's children
				orNode.children = children.ToArray();
				ret = orNode;
				changed |= true; // Mark that a change has occurred

			// If all child nodes are OR nodes and Literal Nodes...
			} else if (Node.allHaveTypes(orNode.children, new Node.Type[]{Node.Type.Or, Node.Type.Literal})){
				// Create a list of all of the literal children combined with the children's children
				List<Node> children = new List<Node>();
				foreach(Node child in orNode.children){
					if(child.type == Node.Type.Literal)
						children.Add(child);
					else if(child.type == Node.Type.Or){
						foreach(Node childsChild in child.children)
							children.Add(childsChild);
					}
				}

				// Our children become the new list of children
				orNode.children = children.ToArray();
				ret = orNode;
				changed |= true; // Mark that a change occured
			}

		// If this node is an AND node...
		} else if(node.type == Node.Type.And){
			ANDNode andNode = node as ANDNode;

			// If all of the children are literals... remove any children of this node that are the same
			if(Node.allHaveType(andNode.children, Node.Type.Literal) && andNode.children is object){
				List<Node> children = new List<Node>(andNode.children);

				// Remove duplicates
				for(int i = 0; i < children.Count; i++)
					for(int j = i + 1; j < children.Count; j++)
						if((children[i] as LiteralNode).details == (children[j] as LiteralNode).details)
							children.RemoveAt(j);

				// If only a single child is remaining, this node just becomes the child
				if(children.Count == 1)
					ret = children[0];
				// Otherwise update the list of children
				else {
					andNode.children = children.ToArray();
					ret = andNode;
				}

				// If the number of children changed... mark that a change has occurred
				if(children.Count != andNode.children.Length)
					changed |= true;

			// If all child nodes are AND nodes...
			} else if(Node.allHaveType(andNode.children, Node.Type.And) && andNode.children is object){
				// Create a list of all of the children's children
				List<Node> children = new List<Node>();
				foreach(Node child in andNode.children)
					foreach(Node childsChild in child.children)
						children.Add(childsChild);

				// Our children become the children's children
				andNode.children = children.ToArray();
				ret = andNode;
				changed |= true; // Mark that a change has occurred

			// If all child nodes are AND nodes and Literal Nodes...
			} else if (Node.allHaveTypes(andNode.children, new Node.Type[]{Node.Type.And, Node.Type.Literal})){
				// Create a list of all of the literal children combined with the children's children
				List<Node> children = new List<Node>();
				foreach(Node child in andNode.children){
					if(child.type == Node.Type.Literal)
						children.Add(child);
					else if(child.type == Node.Type.Or){
						foreach(Node childsChild in child.children)
							children.Add(childsChild);
					}
				}

				// Our children become the new list of children
				andNode.children = children.ToArray();
				ret = andNode;
				changed |= true; // Mark that a change has occured
			}
		}

		return ret;
	}


	// functions which repeatedly combines and expands literals until no change has occurred
	Node combineAndExpandAllLiterals(Node node){
		bool changed = true;
		while(changed){
			changed = false;
			node = combineAndExpandAllLiterals(node, ref changed);
		}

		return node;
	}

	// Function which combines all literal nodes in the tree (passes out if a change occurred)
	Node combineAndExpandAllLiterals(Node node, ref bool changed){
		node = combineAndExpandLiterals(node, ref changed);
		if(node.children is object)
			 for(int i = 0; i < node.children.Length; i++)
				node.children[i] = combineAndExpandAllLiterals(node.children[i], ref changed);

		return node;
	}

	// Function which combines and expands all child literanl nodes
	Node combineAndExpandLiterals(Node input, ref bool changed){
		// If the input node is an AND node...
		if(input.type == Node.Type.And){
			// If all of the children are literal nodes
			if(Node.allHaveType(input.children, Node.Type.Literal)){
				// Options of the new input node
				Color outColor = Color.Any;
				Size outSize = Size.Any;
				Shape outShape = Shape.Any;

				// For each child... check the details of the node for any matching rules
				foreach(Node child in input.children){
					Details details = (child as LiteralNode).details;

					// If there is a color mark it as this node's color (if a color has already been marked error)
					if(details.color != Color.Any){
						if(outColor != Color.Any)
							throw new System.ArgumentException("Input resulted in a combination of multiple conflicting color values.");
						else outColor = details.color;
					}

					// If there is a size mark it as this node's size (if a size has already been marked error)
					if(details.size != Size.Any){
						if(outSize != Size.Any)
							throw new System.ArgumentException("Input resulted in a combination of multiple conflicting size values.");
						else outSize = details.size;
					}

					// If there is a shape mark it as this node's shape (if a shape has already been marked error)
					if(details.shape != Shape.Any){
						if(outShape != Shape.Any)
							throw new System.ArgumentException("Input resulted in a combination of multiple conflicting shape values.");
						else outShape = details.shape;
					}
				}

				// Return a new node with the combined rules
				LiteralNode ret = new LiteralNode();
				ret.details = new Details(outColor, outSize, outShape);
				changed |= true; // Mark that a change has occurred
				return ret;
			}

		// If the input node is an OR node...
		} else if(input.type == Node.Type.Or){
			// If all of the children are literels
			if(Node.allHaveType(input.children, Node.Type.Literal)){
				// Create a list of all the expanded literals in the children
				List<Details> childrenDetails = new List<Details>();

				// For each child
				foreach(Node c in input.children)
					// Add the created list for this node to the total list
					childrenDetails.AddRange(expandSingleLiteralNode(c as LiteralNode));

				// Convert the list of details to a list of distinct literal nodes
				LiteralNode[] _new = LiteralNode.fromDetails(childrenDetails.Distinct());
				// If the new list of children is a different size from the old list of children... mark that an change has occurred
				if(_new.Length != input.children.Length)
					changed |= true;
				// Update our children with the new list of children
				input.children = _new;
			}
		}

		return input;
	}

	// Function which expands a singular LiteralNode
	List<Details> expandSingleLiteralNode(LiteralNode node){
		// Get its details
		Details details = node.details;
		// Create a list of all of its expanded rules
		List<Details> dList = new List<Details>();

		// Expand color
		if(details.color != Color.Any)
			dList.Add(new Details(details.color, Size.Any, Shape.Any));
		else {
			dList.Add(new Details(Color.Pink, Size.Any, Shape.Any));
			dList.Add(new Details(Color.Blue, Size.Any, Shape.Any));
			dList.Add(new Details(Color.Green, Size.Any, Shape.Any));
		}

		// Expand size
		if(details.size != Size.Any){
			for(int i = 0; i < dList.Count; i++){
				Details d = dList[i];
				d.size = details.size;
				dList[i] = d;
			}
		} else {
			List<Details> oldDList = dList;
			dList = new List<Details>();

			foreach(Details d in oldDList){
				dList.Add(new Details(d.color, Size.Small, Shape.Any));
				dList.Add(new Details(d.color, Size.Medium, Shape.Any));
				dList.Add(new Details(d.color, Size.Large, Shape.Any));
			}
		}

		// Expand shape
		if(details.shape != Shape.Any){
			for(int i = 0; i < dList.Count; i++){
				Details d = dList[i];
				d.shape = details.shape;
				dList[i] = d;
			}
		} else {
			List<Details> oldDList = dList;
			dList = new List<Details>();

			foreach(Details d in oldDList){
				dList.Add(new Details(d.color, d.size, Shape.Sphere));
				dList.Add(new Details(d.color, d.size, Shape.Cone));
				dList.Add(new Details(d.color, d.size, Shape.Cube));
			}
		}

		return dList;
	}
}
