OUT_FORMAT = beamer
THEME = -V theme:default -V colortheme:crane -V fonttheme:structurebold
PANDOC_OPTS = -t $(OUT_FORMAT) $(THEME) --slide-level 2

.PHONY: all clean

all: PRESENTATION.md

PRESENTATION.md:
	cd src && pandoc $(PANDOC_OPTS) -o PRESENTATION.pdf PRESENTATION.md
	evince src/PRESENTATION.pdf

clean:
	-@rm -f src/*.pdf
