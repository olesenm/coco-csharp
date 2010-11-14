all:
	gmcs /out:Coco.exe /t:exe Coco.cs Scanner.cs Tab.cs DFA.cs ParserGen.cs Parser.cs 

clean:
	rm -f Coco.exe

install:
	install -m 0755 cococs $(DESTDIR)/usr/bin
	install -m 0755 Coco.exe $(DESTDIR)/usr/share/coco-cs
	install -m 0644 *frame $(DESTDIR)/usr/share/coco-cs

