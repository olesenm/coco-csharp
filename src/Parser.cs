/*---------------------------------------------------------------------------*\
    Compiler Generator Coco/R,
    Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
    extended by M. Loeberbauer & A. Woess, Univ. of Linz
    with improvements by Pat Terry, Rhodes University
-------------------------------------------------------------------------------
License
    This file is part of Compiler Generator Coco/R

    This program is free software; you can redistribute it and/or modify it
    under the terms of the GNU General Public License as published by the
    Free Software Foundation; either version 2, or (at your option) any
    later version.

    This program is distributed in the hope that it will be useful, but
    WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
    or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
    for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.

    As an exception, it is allowed to write an extension of Coco/R that is
    used as a plugin in non-free software.

    If not otherwise stated, any source code generated by Coco/R (other than
    Coco/R itself) does not fall under the GNU General Public License.
\*---------------------------------------------------------------------------*/
using System.IO;



using System;

namespace at.jku.ssw.Coco {



public class Parser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _number = 2;
	public const int _string = 3;
	public const int _badString = 4;
	public const int _char = 5;
	public const int maxT = 43;
	public const int _ddtSym = 44;
	public const int _directive = 45;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;

	public Scanner scanner;
	public Errors  errors;

	public Token t;    //!< last recognized token
	public Token la;   //!< lookahead token
	int errDist = minErrDist;

const int isIdent   = 0;
	const int isLiteral = 1;

	public Tab tab;             // other Coco objects referenced in this ATG
	public DFA dfa;
	public ParserGen pgen;

	bool   genScanner = false;
	string tokenString;         // used in declarations of literal tokens
	string noString = "-none-"; // used in declarations of literal tokens

/*-------------------------------------------------------------------------*/



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}

	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }
				if (la.kind == 44) {
				tab.SetDDT(la.val);
				}
				if (la.kind == 45) {
				tab.DispatchDirective(la.val);
				}

			la = t;
		}
	}

	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}

	bool StartOf (int s) {
		return set[s, la.kind];
	}

	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}


	void Coco() {
		Symbol sym; Graph g; string grammarName; CharSet s;
		if (la.kind == 6) {
			Get();
			int beg = t.pos + t.val.Length;
			while (StartOf(1)) {
				Get();
			}
			tab.copyPos = new Position(beg, la.pos, 0);
			Expect(7);
		}
		if (StartOf(2)) {
			Get();
			int beg = t.pos;
			while (StartOf(3)) {
				Get();
			}
			pgen.preamblePos = new Position(beg, la.pos, 0);
		}
		Expect(8);
		genScanner = true;
		Expect(1);
		grammarName = t.val;
		if (StartOf(4)) {
			Get();
			int beg = t.pos;
			while (StartOf(4)) {
				Get();
			}
			pgen.semDeclPos = new Position(beg, la.pos, 0);
		}
		if (la.kind == 9) {
			Get();
			dfa.ignoreCase = true;
		}
		if (la.kind == 10) {
			Get();
			while (la.kind == 1) {
				SetDecl();
			}
		}
		if (la.kind == 11) {
			Get();
			while (la.kind == 1 || la.kind == 3 || la.kind == 5) {
				TokenDecl(Node.t);
			}
		}
		if (la.kind == 12) {
			Get();
			while (la.kind == 1 || la.kind == 3 || la.kind == 5) {
				TokenDecl(Node.pr);
			}
		}
		while (la.kind == 13) {
			Get();
			Graph g1, g2; bool nested = false;
			Expect(14);
			TokenExpr(out g1);
			Expect(15);
			TokenExpr(out g2);
			if (la.kind == 16) {
				Get();
				nested = true;
			}
			dfa.NewComment(g1.l, g2.l, nested);
		}
		while (la.kind == 17) {
			Get();
			Set(out s);
			tab.ignored.Or(s);
		}
		while (!(la.kind == 0 || la.kind == 18)) {SynErr(44); Get();}
		Expect(18);
		if (genScanner) dfa.MakeDeterministic();
		tab.DeleteNodes();

		while (la.kind == 1) {
			Get();
			sym = tab.FindSym(t.val);
			bool undef = (sym == null);
			if (undef) sym = tab.NewSym(Node.nt, t.val, t.line);
			else {
			  if (sym.typ == Node.nt) {
			    if (sym.graph != null)
			      SemErr("name declared twice");
			  } else SemErr("this symbol kind not allowed on left side of production");
			  sym.line = t.line;
			}
			bool noAttrs = (sym.attrPos == null);
			sym.attrPos = null;

			if (la.kind == 26 || la.kind == 28) {
				AttrDecl(sym);
			}
			if (!undef && noAttrs != (sym.attrPos == null))
			 SemErr("attribute mismatch between declaration and use of this symbol");

			if (la.kind == 41) {
				SemText(out sym.semPos);
			}
			ExpectWeak(19, 5);
			Expression(out g);
			sym.graph = g.l;
			tab.Finish(g);

			ExpectWeak(20, 6);
		}
		Expect(21);
		Expect(1);
		if (grammarName != t.val)
		 SemErr("name does not match grammar name");
		tab.gramSy = tab.FindSym(grammarName);
		if (tab.gramSy == null)
		  SemErr("missing production for grammar name");
		else {
		  sym = tab.gramSy;
		  if (sym.attrPos != null)
		    SemErr("grammar symbol must not have attributes");
		}
		tab.noSym = tab.NewSym(Node.t, "???", 0); // noSym gets highest number
		tab.SetupAnys();
		tab.RenumberPragmas();
		if (tab.ddt[2]) tab.PrintNodes();
		if (errors.count == 0) {
		  Console.WriteLine("checking");
		  tab.CompSymbolSets();
		  if (tab.ddt[7]) tab.XRef();
		  if (tab.GrammarOk()) {
		    Console.Write("parser");
		    pgen.WriteParser();
		    if (genScanner) {
		      Console.Write(" + scanner");
		      dfa.WriteScanner();
		      if (tab.ddt[0]) dfa.PrintStates();
		    }
		    Console.WriteLine(" generated");
		    if (tab.ddt[8]) {
		      tab.PrintStatistics();
		      pgen.PrintStatistics();
		    }
		  }
		}
		if (tab.ddt[6]) tab.PrintSymbolTable();

		Expect(20);
	}

	void SetDecl() {
		CharSet s;
		Expect(1);
		string name = t.val;
		CharClass c = tab.FindCharClass(name);
		if (c != null) SemErr("name declared twice");

		Expect(19);
		Set(out s);
		if (s.Elements() == 0) SemErr("character set must not be empty");
		tab.NewCharClass(name, s);

		Expect(20);
	}

	void TokenDecl(int typ) {
		string name; int kind; Symbol sym; Graph g;
		Sym(out name, out kind);
		sym = tab.FindSym(name);
		if (sym != null) SemErr("name declared twice");
		else {
		  sym = tab.NewSym(typ, name, t.line);
		  sym.tokenKind = Symbol.fixedToken;
		}
		tokenString = null;

		while (!(StartOf(7))) {SynErr(45); Get();}
		if (la.kind == 19) {
			Get();
			TokenExpr(out g);
			Expect(20);
			if (kind == isLiteral) SemErr("a literal must not be declared with a structure");
			tab.Finish(g);
			if (tokenString == null || tokenString.Equals(noString))
			  dfa.ConvertToStates(g.l, sym);
			else { // TokenExpr is a single string
			  if (tab.literals[tokenString] != null)
			    SemErr("token string declared twice");
			  tab.literals[tokenString] = sym;
			  dfa.MatchLiteral(tokenString, sym);
			}

		} else if (StartOf(8)) {
			if (kind == isIdent) genScanner = false;
			else dfa.MatchLiteral(sym.name, sym);

		} else SynErr(46);
		if (la.kind == 41) {
			SemText(out sym.semPos);
			if (typ != Node.pr) SemErr("semantic action not allowed here");
		}
	}

	void TokenExpr(out Graph g) {
		Graph g2;
		TokenTerm(out g);
		bool first = true;
		while (WeakSeparator(30,9,10) ) {
			TokenTerm(out g2);
			if (first) { tab.MakeFirstAlt(g); first = false; }
			tab.MakeAlternative(g, g2);

		}
	}

	void Set(out CharSet s) {
		CharSet s2;
		SimSet(out s);
		while (la.kind == 22 || la.kind == 23) {
			if (la.kind == 22) {
				Get();
				SimSet(out s2);
				s.Or(s2);
			} else {
				Get();
				SimSet(out s2);
				s.Subtract(s2);
			}
		}
	}

	void AttrDecl(Symbol sym) {
		if (la.kind == 26) {
			Get();
			int beg = la.pos; int col = la.col;
			while (StartOf(11)) {
				if (StartOf(12)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes");
				}
			}
			Expect(27);
			if (t.pos > beg)
			 sym.attrPos = new Position(beg, t.pos, col);
		} else if (la.kind == 28) {
			Get();
			int beg = la.pos; int col = la.col;
			while (StartOf(13)) {
				if (StartOf(14)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes");
				}
			}
			Expect(29);
			if (t.pos > beg)
			 sym.attrPos = new Position(beg, t.pos, col);
		} else SynErr(47);
	}

	void SemText(out Position pos) {
		Expect(41);
		int beg = la.pos; int col = la.col;
		while (StartOf(15)) {
			if (StartOf(16)) {
				Get();
			} else if (la.kind == 4) {
				Get();
				SemErr("bad string in semantic action");
			} else {
				Get();
				SemErr("missing end of previous semantic action");
			}
		}
		Expect(42);
		pos = new Position(beg, t.pos, col);
	}

	void Expression(out Graph g) {
		Graph g2;
		Term(out g);
		bool first = true;
		while (WeakSeparator(30,17,18) ) {
			Term(out g2);
			if (first) { tab.MakeFirstAlt(g); first = false; }
			tab.MakeAlternative(g, g2);

		}
	}

	void SimSet(out CharSet s) {
		int n1, n2; s = new CharSet();
		if (la.kind == 1) {
			Get();
			CharClass c = tab.FindCharClass(t.val);
			if (c == null) SemErr("undefined name"); else s.Or(c.set);

		} else if (la.kind == 3) {
			Get();
			string name = t.val;
			name = tab.Unescape(name.Substring(1, name.Length-2));
			foreach (char ch in name)
			  if (dfa.ignoreCase) s.Set(char.ToLower(ch));
			  else s.Set(ch);
		} else if (la.kind == 5) {
			Char(out n1);
			s.Set(n1);
			if (la.kind == 24) {
				Get();
				Char(out n2);
				for (int i = n1; i <= n2; i++) s.Set(i);
			}
		} else if (la.kind == 25) {
			Get();
			s = new CharSet(); s.Fill();
		} else SynErr(48);
	}

	void Char(out int n) {
		Expect(5);
		string name = t.val; n = 0;
		name = tab.Unescape(name.Substring(1, name.Length-2));
		if (name.Length == 1) n = name[0];
		else SemErr("unacceptable character value");
		if (dfa.ignoreCase && (char)n >= 'A' && (char)n <= 'Z') n += 32;

	}

	void Sym(out string name, out int kind) {
		name = "???"; kind = isIdent;
		if (la.kind == 1) {
			Get();
			kind = isIdent; name = t.val;
		} else if (la.kind == 3 || la.kind == 5) {
			if (la.kind == 3) {
				Get();
				name = t.val;
			} else {
				Get();
				name = "\"" + t.val.Substring(1, t.val.Length-2) + "\"";
			}
			kind = isLiteral;
			if (dfa.ignoreCase) name = name.ToLower();
			if (name.IndexOf(' ') >= 0)
			  SemErr("literal tokens must not contain blanks");
		} else SynErr(49);
	}

	void Term(out Graph g) {
		Graph g2; Node rslv = null; g = null;
		if (StartOf(19)) {
			if (la.kind == 39) {
				rslv = tab.NewNode(Node.rslv, null, la.line);
				Resolver(out rslv.pos);
				g = new Graph(rslv);
			}
			Factor(out g2);
			if (rslv != null) tab.MakeSequence(g, g2);
			else g = g2;

			while (StartOf(20)) {
				Factor(out g2);
				tab.MakeSequence(g, g2);
			}
		} else if (StartOf(21)) {
			g = new Graph(tab.NewNode(Node.eps, null, 0));
		} else SynErr(50);
		if (g == null) // invalid start of Term
		 g = new Graph(tab.NewNode(Node.eps, null, 0));

	}

	void Resolver(out Position pos) {
		Expect(39);
		Expect(32);
		int beg = la.pos; int col = la.col;
		Condition();
		pos = new Position(beg, t.pos, col);
	}

	void Factor(out Graph g) {
		string name; int kind; Position pos; bool weak = false;
		g = null;

		switch (la.kind) {
		case 1: case 3: case 5: case 31: {
			if (la.kind == 31) {
				Get();
				weak = true;
			}
			Sym(out name, out kind);
			Symbol sym = tab.FindSym(name);
			if (sym == null && kind == isLiteral)
			  sym = tab.literals[name] as Symbol;
			bool undef = (sym == null);
			if (undef) {
			  if (kind == isIdent)
			    sym = tab.NewSym(Node.nt, name, 0);  // forward nt
			  else if (genScanner) {
			    sym = tab.NewSym(Node.t, name, t.line);
			    dfa.MatchLiteral(sym.name, sym);
			  } else {  // undefined string in production
			    SemErr("undefined string in production");
			    sym = tab.eofSy;  // dummy
			  }
			}
			int typ = sym.typ;
			if (typ != Node.t && typ != Node.nt)
			  SemErr("this symbol kind is not allowed in a production");
			if (weak)
			  if (typ == Node.t) typ = Node.wt;
			  else SemErr("only terminals may be weak");
			Node p = tab.NewNode(typ, sym, t.line);
			g = new Graph(p);

			if (la.kind == 26 || la.kind == 28) {
				Attribs(p);
				if (kind == isLiteral) SemErr("a literal must not have attributes");
			}
			if (undef)
			 sym.attrPos = p.pos;  // dummy
			else if ((p.pos == null) != (sym.attrPos == null))
			  SemErr("attribute mismatch between declaration and use of this symbol");

			break;
		}
		case 32: {
			Get();
			Expression(out g);
			Expect(33);
			break;
		}
		case 34: {
			Get();
			Expression(out g);
			Expect(35);
			tab.MakeOption(g);
			break;
		}
		case 36: {
			Get();
			Expression(out g);
			Expect(37);
			tab.MakeIteration(g);
			break;
		}
		case 41: {
			SemText(out pos);
			Node p = tab.NewNode(Node.sem, null, 0);
			p.pos = pos;
			g = new Graph(p);

			break;
		}
		case 25: {
			Get();
			Node p = tab.NewNode(Node.any, null, 0);  // p.set is set in tab.SetupAnys
			g = new Graph(p);

			break;
		}
		case 38: {
			Get();
			Node p = tab.NewNode(Node.sync, null, 0);
			g = new Graph(p);

			break;
		}
		default: SynErr(51); break;
		}
		if (g == null) // invalid start of Factor
		 g = new Graph(tab.NewNode(Node.eps, null, 0));

	}

	void Attribs(Node p) {
		if (la.kind == 26) {
			Get();
			int beg = la.pos; int col = la.col;
			while (StartOf(11)) {
				if (StartOf(12)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes");
				}
			}
			Expect(27);
			if (t.pos > beg) p.pos = new Position(beg, t.pos, col);
		} else if (la.kind == 28) {
			Get();
			int beg = la.pos; int col = la.col;
			while (StartOf(13)) {
				if (StartOf(14)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes");
				}
			}
			Expect(29);
			if (t.pos > beg) p.pos = new Position(beg, t.pos, col);
		} else SynErr(52);
	}

	void Condition() {
		while (StartOf(22)) {
			if (la.kind == 32) {
				Get();
				Condition();
			} else {
				Get();
			}
		}
		Expect(33);
	}

	void TokenTerm(out Graph g) {
		Graph g2;
		TokenFactor(out g);
		while (StartOf(9)) {
			TokenFactor(out g2);
			tab.MakeSequence(g, g2);
		}
		if (la.kind == 40) {
			Get();
			Expect(32);
			TokenExpr(out g2);
			tab.SetContextTrans(g2.l);
			dfa.hasCtxMoves = true;
			tab.MakeSequence(g, g2);
			Expect(33);
		}
	}

	void TokenFactor(out Graph g) {
		string name; int kind; g = null;
		if (la.kind == 1 || la.kind == 3 || la.kind == 5) {
			Sym(out name, out kind);
			if (kind == isIdent) {
			 CharClass c = tab.FindCharClass(name);
			 if (c == null) {
			   SemErr("undefined name");
			   c = tab.NewCharClass(name, new CharSet());
			 }
			 Node p = tab.NewNode(Node.clas, null, 0); p.val = c.n;
			 g = new Graph(p);
			 tokenString = noString;
			} else { // str
			  g = tab.StrToGraph(name);
			  if (tokenString == null) tokenString = name;
			  else tokenString = noString;
			}

		} else if (la.kind == 32) {
			Get();
			TokenExpr(out g);
			Expect(33);
		} else if (la.kind == 34) {
			Get();
			TokenExpr(out g);
			Expect(35);
			tab.MakeOption(g); tokenString = noString;
		} else if (la.kind == 36) {
			Get();
			TokenExpr(out g);
			Expect(37);
			tab.MakeIteration(g); tokenString = noString;
		} else SynErr(53);
		if (g == null) // invalid start of TokenFactor
		 g = new Graph(tab.NewNode(Node.eps, null, 0));
	}



	public void Parse() {
		la = new Token();
		la.val = "";
		Get();
		Coco();
		Expect(0); // expect end-of-file automatically added

	}

	static readonly bool[,] set = {
		{T,T,x,T, x,T,x,x, x,x,x,x, T,T,x,x, x,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x},
		{x,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{x,T,T,T, T,T,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{x,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{x,T,T,T, T,T,T,T, T,x,x,x, x,x,T,T, T,x,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{T,T,x,T, x,T,x,x, x,x,x,x, T,T,x,x, x,T,T,T, T,x,x,x, x,T,x,x, x,x,T,T, T,x,T,x, T,x,T,T, x,T,x,x, x},
		{T,T,x,T, x,T,x,x, x,x,x,x, T,T,x,x, x,T,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x},
		{T,T,x,T, x,T,x,x, x,x,x,x, T,T,x,x, x,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x},
		{x,T,x,T, x,T,x,x, x,x,x,x, T,T,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x},
		{x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, T,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,T,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,T,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{x,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{x,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,T, x},
		{x,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,T, x},
		{x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,T, T,T,T,T, T,T,T,T, x,T,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,T,x,x, x,x,x,x, x},
		{x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, T,x,T,x, T,x,T,T, x,T,x,x, x},
		{x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, T,x,T,x, T,x,T,x, x,T,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,T,x,T, x,T,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,T,T,T, T,T,T,T, x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	static public string strerror(int n) {
		switch (n) {
			case 0: return "EOF expected";
			case 1: return "ident expected";
			case 2: return "number expected";
			case 3: return "string expected";
			case 4: return "badString expected";
			case 5: return "char expected";
			case 6: return "\"[copy]\" expected";
			case 7: return "\"[/copy]\" expected";
			case 8: return "\"COMPILER\" expected";
			case 9: return "\"IGNORECASE\" expected";
			case 10: return "\"CHARACTERS\" expected";
			case 11: return "\"TOKENS\" expected";
			case 12: return "\"PRAGMAS\" expected";
			case 13: return "\"COMMENTS\" expected";
			case 14: return "\"FROM\" expected";
			case 15: return "\"TO\" expected";
			case 16: return "\"NESTED\" expected";
			case 17: return "\"IGNORE\" expected";
			case 18: return "\"PRODUCTIONS\" expected";
			case 19: return "\"=\" expected";
			case 20: return "\".\" expected";
			case 21: return "\"END\" expected";
			case 22: return "\"+\" expected";
			case 23: return "\"-\" expected";
			case 24: return "\"..\" expected";
			case 25: return "\"ANY\" expected";
			case 26: return "\"<\" expected";
			case 27: return "\">\" expected";
			case 28: return "\"<.\" expected";
			case 29: return "\".>\" expected";
			case 30: return "\"|\" expected";
			case 31: return "\"WEAK\" expected";
			case 32: return "\"(\" expected";
			case 33: return "\")\" expected";
			case 34: return "\"[\" expected";
			case 35: return "\"]\" expected";
			case 36: return "\"{\" expected";
			case 37: return "\"}\" expected";
			case 38: return "\"SYNC\" expected";
			case 39: return "\"IF\" expected";
			case 40: return "\"CONTEXT\" expected";
			case 41: return "\"(.\" expected";
			case 42: return "\".)\" expected";
			case 43: return "??? expected";
			case 44: return "this symbol not expected in Coco";
			case 45: return "this symbol not expected in TokenDecl";
			case 46: return "invalid TokenDecl";
			case 47: return "invalid AttrDecl";
			case 48: return "invalid SimSet";
			case 49: return "invalid Sym";
			case 50: return "invalid Term";
			case 51: return "invalid Factor";
			case 52: return "invalid Attribs";
			case 53: return "invalid TokenFactor";

			default: return "error " + n;
		}
	}

	public void SynErr (int line, int col, int n) {
		errorStream.WriteLine(errMsgFormat, line, col, strerror(n));
		count++;
	}

	public void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}

	public void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}

	public void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}

} // end namespace
