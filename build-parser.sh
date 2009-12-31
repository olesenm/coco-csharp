#!/bin/sh
#------------------------------------------------------------------------------
cd ${0%/*} || exit 1    # run from this directory

echo "create Parser.cs and Scanner.cs for the Coco grammar"
echo

namespace=at.jku.ssw.Coco

echo "mono Coco.net Coco.atg -namespace $namespace"

mono Coco.net Coco.atg -namespace $namespace
echo

# ----------------------------------------------------------------- end-of-file
