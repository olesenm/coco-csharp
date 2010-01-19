/*-------------------------------------------------------------------------
Compiler Generator Coco/R,
Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
extended by M. Loeberbauer & A. Woess, Univ. of Linz
with improvements by Pat Terry, Rhodes University

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
-------------------------------------------------------------------------*/
/*-------------------------------------------------------------------------
  Trace output options
  0 | A: prints the states of the scanner automaton
  1 | F: prints the First and Follow sets of all nonterminals
  2 | G: prints the syntax graph of the productions
  3 | I: traces the computation of the First sets
  4 | J: prints the sets associated with ANYs and synchronisation sets
  6 | S: prints the symbol table (terminals, nonterminals, pragmas)
  7 | X: prints a cross reference list of all syntax symbols
  8 | P: prints statistics about the Coco run

  Trace output can be switched on by the pragma
    $ { digit | letter }
  in the attributed grammar or as a command-line option
  -------------------------------------------------------------------------*/

using System;
using System.IO;

namespace at.jku.ssw.Coco {

public class Coco {

	public static void printUsage (string message) {
		if (message != null) Console.WriteLine(message);

		Console.WriteLine
		(
			"Usage: Coco Grammar.atg {{Option}}{0}" +
			"Options:{0}" +
			"  -namespace <Name>      eg, My.Name.Space{0}" +
			"  -prefix    <Name>      for unique Parser/Scanner file names{0}" +
			"  -frames    <Dir>       for frames not in the source directory{0}" +
			"  -trace     <String>    trace with output to trace.txt{0}" +
			"  -trace2    <String>    trace with output on stderr{0}" +
			"  -o         <Dir>       output directory{0}" +
			"  -bak                   save existing Parser/Scanner files as .bak{0}" +
			"  -help                  print this usage{0}" +
			"{0}Valid characters in the trace string:{0}" +
			"  A  trace automaton{0}" +
			"  F  list first/follow sets{0}" +
			"  G  print syntax graph{0}" +
			"  I  trace computation of first sets{0}" +
			"  J  list ANY and SYNC sets{0}" +
			"  P  print statistics{0}" +
			"  S  list symbol table{0}" +
			"  X  list cross reference table{0}" +
			"Scanner.frame and Parser.frame must be located in one of these directories:{0}" +
			"  1. The same directory as the atg grammar.{0}" +
			"  2. In the specified -frames directory.{0}" +
			"{0}http://www.ssw.uni-linz.ac.at/coco/{0}{0}",
			Environment.NewLine
		);
	}

	public static int Main (string[] arg) {
		Console.WriteLine("Coco/R C# (19 Jan 2010)");
		string srcName = null, nsName = null, prefixName = null;
		string frameDir = null, ddtString = null, outDir = null;
		bool makeBackup = false;
		bool traceToFile = true;
		int retVal = 1;

		for (int i = 0; i < arg.Length; i++) {
			if (arg[i] == "-help") {
				printUsage(null);
				return 0;
			}
			else if (arg[i] == "-namespace") {
				if (++i == arg.Length) {
					printUsage("missing parameter on -namespace");
					return retVal;
				}
				nsName = arg[i];
			}
			else if (arg[i] == "-prefix") {
				if (++i == arg.Length) {
					printUsage("missing parameter on -prefix");
					return retVal;
				}
				prefixName = arg[i];
			}
			else if (arg[i] == "-frames") {
				if (++i == arg.Length) {
					printUsage("missing parameter on -frames");
					return retVal;
				}
				frameDir = arg[i];
			}
			else if (arg[i] == "-trace") {
				if (++i == arg.Length) {
					printUsage("missing parameter on -trace");
					return retVal;
				}
				traceToFile = true;
				ddtString = arg[i];
			}
			else if (arg[i] == "-trace2") {
				if (++i == arg.Length) {
					printUsage("missing parameter on -trace2");
					return retVal;
				}
				traceToFile = false;
				ddtString = arg[i];
			}
			else if (arg[i] == "-o") {
				if (++i == arg.Length) {
					printUsage("missing parameter on -o");
					return retVal;
				}
				outDir = arg[i];
			}
			else if (arg[i] == "-bak") {
				makeBackup = true;
			}
			else if (arg[i][0] == '-') {
				printUsage("Error: unknown option: '" + arg[i] + "'");
				return retVal;
			}
			else if (srcName != null) {
				printUsage("grammar can only be specified once");
				return retVal;
			}
			else {
				srcName = arg[i];
			}
		}

		if (srcName != null) {
			string traceFileName = null;
			try {
				string srcDir = Path.GetDirectoryName(srcName);

				Scanner scanner = new Scanner(srcName);
				Parser parser   = new Parser(scanner);
				parser.tab      = new Tab(parser);

				parser.tab.srcName    = srcName;
				parser.tab.srcDir     = srcDir;
				parser.tab.nsName     = nsName;
				parser.tab.prefixName = prefixName;
				parser.tab.frameDir = frameDir;
				parser.tab.outDir   = (outDir != null) ? outDir : srcDir;
				parser.tab.SetDDT(ddtString);
				parser.tab.makeBackup = makeBackup;

				if (traceToFile) {
					traceFileName = Path.Combine(parser.tab.outDir, "trace.txt");
					parser.tab.trace = new StreamWriter(new FileStream(traceFileName, FileMode.Create));
				}
				parser.dfa = new DFA(parser);
				parser.pgen = new ParserGen(parser);

				parser.Parse();

				if (traceToFile) {
					parser.tab.trace.Close();
					FileInfo f = new FileInfo(traceFileName);
					if (f.Length == 0) f.Delete();
					else Console.WriteLine("trace output is in " + traceFileName);
				}
				Console.WriteLine("{0} errors detected", parser.errors.count);
				if (parser.errors.count == 0) { retVal = 0; }
			} catch (IOException) {
				Console.WriteLine("-- could not open " + traceFileName);
			} catch (FatalError e) {
				Console.WriteLine("-- " + e.Message);
			}
		} else {
			printUsage(null);
		}
		return retVal;
	}

} // end Coco

} // end namespace

// ************************************************************************* //
