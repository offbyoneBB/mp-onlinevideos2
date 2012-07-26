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

#include "StdAfx.h"

#include "BootstrapInfoBox.h"

CBootstrapInfoBox::CBootstrapInfoBox(void)
  : CBox()
{
  this->version = 0;
  this->flags = 0;
  this->bootstrapInfoVersion = 0;
  this->profile = 0;
  this->live = false;
  this->update = false;
  this->timeScale = 0;
  this->currentMediaTime = 0;
  this->smpteTimeCodeOffset;
  this->movieIdentifier = NULL;
  this->serverEntryTable = new CBootstrapInfoServerEntryCollection();
  this->qualityEntryTable = new CBootstrapInfoQualityEntryCollection();
  this->drmData = NULL;
  this->metaData = NULL;
}

CBootstrapInfoBox::~CBootstrapInfoBox(void)
{
  FREE_MEM(this->movieIdentifier);
  FREE_MEM(this->drmData);
  FREE_MEM(this->metaData);
  if (this->serverEntryTable != NULL)
  {
    delete this->serverEntryTable;
  }
  if (this->qualityEntryTable != NULL)
  {
    delete this->qualityEntryTable;
  }
}

bool CBootstrapInfoBox::Parse(const unsigned char *buffer, unsigned int length)
{
  this->version = 0;
  this->flags = 0;
  this->bootstrapInfoVersion = 0;
  this->profile = 0;
  this->live = false;
  this->update = false;
  this->timeScale = 0;
  this->currentMediaTime = 0;
  this->smpteTimeCodeOffset;
  FREE_MEM(this->movieIdentifier);
  this->serverEntryTable->Clear();
  this->qualityEntryTable->Clear();
  FREE_MEM(this->drmData);
  FREE_MEM(this->metaData);

  bool result = __super::Parse(buffer, length);
  if (result)
  {
    if (wcscmp(this->type, BOOTSTRAP_INFO_BOX_TYPE) != 0)
    {
      // incorect box type
      this->parsed = false;
      result = false;
    }
  }

  return result;
}
