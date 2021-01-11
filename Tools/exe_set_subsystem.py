#!/usr/bin/env python3

# exe_set_subsystem

# Copyright (c) 2020 20kdc
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.

import sys
import struct

if len(sys.argv) != 3:
	print("exe_set_subsystem.py <EXE> <SUBSYSTEM>")
	print(" alters EXE in-place to change it's subsystem to SUBSYSTEM")
	print("")
	print("SUBSYSTEM values:")
	print(" 2: GUI")
	print(" 3: Console")
	sys.exit(1)

file = open(sys.argv[1], "r+b")
if file.read(2) != b"MZ":
	print("Header must be 'MZ'.")
	sys.exit(2)
file.seek(0x3C)

peSignatureOfs = struct.unpack("<I", file.read(4))[0]
print("PE Signature Ofs: " + str(peSignatureOfs))
file.seek(peSignatureOfs)
if file.read(4) != b"PE\x00\x00":
	print("PE Signature must be 'PE'.")
	sys.exit(3)
peHeader = struct.unpack("<HHIIIHH", file.read(20))
optHeaderOfs = peSignatureOfs + 4 + 20
print("Opt. Header Ofs: " + str(optHeaderOfs))

subsystemOptOfs = 0
# This is the hard bit.
# Or not.
# Expected these to be in different places.
if peHeader[0] == 332:
	subsystemOptOfs = 68
elif peHeader[0] == 0x8664:
	subsystemOptOfs = 68
else:
	print("Unable to handle machine: " + str(peHeader[0]))
	sys.exit(4)
subsystemOfs = optHeaderOfs + subsystemOptOfs
file.seek(subsystemOfs)
print("Current Subsystem: " + str(struct.unpack("<H", file.read(2))[0]))
file.seek(subsystemOfs)
file.write(struct.pack("<H", int(sys.argv[2])))
file.close()
print("Done!")

