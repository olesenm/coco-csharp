#!/bin/sh
#------------------------------------------------------------------------------
cd ${0%/*} || exit 1    # run from this directory

echo "create Parser.cs and Scanner.cs for the Coco grammar"
echo "    mono Coco.net Coco.atg"
echo

mono Coco.net Coco.atg
echo

# ----------------------------------------------------------------- end-of-file
