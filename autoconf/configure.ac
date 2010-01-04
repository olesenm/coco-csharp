#                                               -*- Autoconf -*-
# Process this file with autoconf to produce a configure script.

AC_PREREQ([2.63])
AC_INIT([Coco/R C-Sharp], [20090104], [Moessenboeck AT ssw.jku.at], [coco-cs])
AC_CONFIG_AUX_DIR(autoconf)
AC_CONFIG_SRCDIR([src/Coco.cs])

# Checks for programs.
AC_PATH_PROG(MCS,  gmcs, no)
AC_PATH_PROG(MONO, mono, no)

if test "$MCS" = "no"; then
    AC_MSG_ERROR([Cannot find "gmcs" in your PATH])
fi
if test "$MONO" = "no"; then
    AC_MSG_ERROR([Cannot find "mono" in your PATH])
fi


AC_PROG_INSTALL

AC_CONFIG_FILES([Makefile:autoconf/Makefile.in src/Makefile])

AC_OUTPUT

# -----------------------------------------------------------------------------
