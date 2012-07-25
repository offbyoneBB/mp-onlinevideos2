/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#pragma once

#ifndef __BASE64_DEFINED
#define __BASE64_DEFINED

#include "MPUrlSourceSplitterExports.h"

// encodes binary input to null-terminated BASE64 encoded string
// caller is responsible of freeing allocated memory by FREE_MEM() method
// @param input : binary input to encode
// @param lenght : the length of binary input
// @param output : reference to output BASE64 encoded string
// @return : S_OK if successfull, E_POINTER if input or output is NULL, E_OUTOFMEMORY if cannot be allocated required memory for output
MPURLSOURCESPLITTER_API HRESULT base64_encode(const unsigned char *input, unsigned int length, char **output);

// decodes BASE64 encoded string to binary output
// caller is responsible of freeing allocated memory by FREE_MEM() method
// @param input : BASE64 encoded string to decode
// @param output : reference to binary output
// @param lenght : reference to the length of binary output
// @return : S_OK if successfull, E_POINTER if input, output or length are NULL, E_INVALIDARG if input contains not valid BASE64 character, E_OUTOFMEMORY if cannot be allocated required memory for output
MPURLSOURCESPLITTER_API HRESULT base64_decode(const char *input, unsigned char **output, unsigned int *length);

#endif