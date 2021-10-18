using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using UnityEngine;

public partial class PacketRule : List<PacketRule.Details> {
	// Enum defining a packet's color
	[Serializable]
	public enum Color {
		Blue,
		Pink,
		Green,
		Any	// Only used by the parser
	}

	// Enum defining a packet's size
	[Serializable]
	public enum Size {
		Invalid = 0,
		Small = 1,
		Medium = 4,
		Large = 6,
		Any	// Only used by the parser
	}

	// Enum defining a packet's shape
	[Serializable]
	public enum Shape {
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
			if (obj == null)
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

		public Node() { parent = null; }

		// Function used to dump tree nodes
		public virtual void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- emptyNode");
		    indent += last ? "   " : "|  ";
		}
	}

	// Or Node
	public class ORNode : Node {
		// Subnodes
		public Node left, right;

		public ORNode() { type = Node.Type.Or; }

		// Function used to dump tree nodes
		public override void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- OR");
		    indent += last ? "   " : "|  ";

		   	left.DebugDump(indent, false);
			right.DebugDump(indent, true);
		}
	}

	// And Nodes
	public class ANDNode: Node {
		// Subnodes
		public Node left, right;

		public ANDNode() { type = Node.Type.And; }

		// Function used to dump tree nodes
		public override void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- AND");
		    indent += last ? "   " : "|  ";

		   	left.DebugDump(indent, false);
			right.DebugDump(indent, true);
		}
	}

	// Not Nodes
	public class NotNode : Node {
		// Subnodes
		public Node child;

		public NotNode() { type = Node.Type.Not; }

		// Function used to dump tree nodes
		public override void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- NOT");
		    indent += last ? "   " : "|  ";

		   	child.DebugDump(indent, true);
		}
	}

	// Literal Nodes
	public class LiteralNode : Node {
		// Details of the literal
		public Details details;

		public LiteralNode() { type = Node.Type.Literal; }

		// Function used to dump tree nodes
		public override void DebugDump(string indent = "", bool last = false) {
		    Debug.Log(indent + "+- " + details);
		    indent += last ? "   " : "|  ";
		}
	}


	// Parse node representing the unoptimized tree;
	public Node treeRoot;


	// -- Parser --


	// Parses a string into a packet rule
	public static PacketRule Parse(string s){
	public static PacketRule Parse(string s, bool autoCommit = true){
		Debug.Log("Input: " + s);

		// Lex the input string down to single characters
		s = lex(s);

		Debug.Log("Lexed: " + s);

		// Parse the lexed tokens into a tree
		int index = 0;
		ParseNode tree = ParseExpression(s, ref index);
		tree.DebugDump();


		return null;
	}

	// Sizes: b = blue, p = pink, g = green
	// Shapes: s = sphere, c = cone, r = rectangle
	// Sizes: t = small, m = medium, l = large
	// Operations: () = parenthesis, & = and, | = or, ! = not
	static char[] validTokens = new char[] {'b', 'p', 'g', 's', 'c', 'r', 't', 'm', 'l', '&', '|', '!', '(', ')'}; // List of valid tokens
 	static string lex(string s){
		// Color
		s = Regex.Replace(s, @"(blue|b)([ &|\(\)])", "b$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(pink|red|p|r)([ &|\(\)])", "p$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(green|g)([ &|\(\)])", "g$+", RegexOptions.IgnoreCase);

		// Shape
		s = Regex.Replace(s, @"(circle|sphere|cir|sp|s)([ &|\(\)])", "s$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(cone|triangle|co|tri|c|t)([ &|\(\)])", "c$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(square|rectangle|cube|box|rect|sq)([ &|\(\)])", "r$+", RegexOptions.IgnoreCase);

		// Size
		s = Regex.Replace(s, @"(small|sm)([ &|\(\)])", "t$+", RegexOptions.IgnoreCase); // S already taken so using the next letter (t)
		s = Regex.Replace(s, @"(medium|med|m)([ &|\(\)])", "m$+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(large|lg|l)([ &|\(\)])", "l$+", RegexOptions.IgnoreCase);

		// Operation
		s = Regex.Replace(s, "(and|conjunction|&&|&)", "&", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(or|disjunction|\|\||\|)", "|", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, "(not|!)", "!", RegexOptions.IgnoreCase);

		// Remove spaces
		s = s.Replace(" ", "");

		// Ensure that only valid tokens remain in the string
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
			(ret as ORNode).left = left;
			(ret as ORNode).right = ParseExpression(tokens, ref index);
			// Mark the output node as the parent to the parsed sub-nodes
			(ret as ORNode).left.parent = ret;
			(ret as ORNode).right.parent = ret;
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
			(ret as NotNode).child = ParseSubexpression(tokens, ref index);
			(ret as NotNode).child.parent = ret;
			return ret;
		}

		// Parse the left hand operand (needs to be parsed first since appears first in grammar)
		Node left = ParseExtendedLiteral(tokens, ref index);

		// If there is an and, create an and node (parsing the right half)
		if(index < tokens.Length && tokens[index] == '&'){
			index++;
			ret = new ANDNode();
			(ret as ANDNode).left = left;
			(ret as ANDNode).right = ParseSubexpression(tokens, ref index);
			// Mark the output node as the parent to the parsed sub-nodes
			(ret as ANDNode).left.parent = ret;
			(ret as ANDNode).right.parent = ret;
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
}
