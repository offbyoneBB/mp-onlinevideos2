<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns="http://schemas.datacontract.org/2004/07/OnlineVideos" version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:i="http://www.w3.org/2001/XMLSchema-instance"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="xml" indent="no" cdata-section-elements="item" omit-xml-declaration="yes"/>

  <xsl:template match="*[local-name() = 'OnlineVideoSites']" priority="2">
    <OnlineVideoSites xmlns="http://schemas.datacontract.org/2004/07/OnlineVideos" xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
      <xsl:apply-templates/>
    </OnlineVideoSites>
  </xsl:template>

  <xsl:template match="*" priority="1">
    <xsl:variable name="elementName" select="local-name()"/>
    <xsl:element name="{$elementName}">
      <xsl:apply-templates select="@*|node()"/>
    </xsl:element>
  </xsl:template>

  <xsl:template match="@i:type" priority="3">
    <xsl:attribute name="i:type">
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>
  
  <xsl:template match="item/@key" priority="3">
    <xsl:attribute name="key">
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>

  <xsl:template match="@*" priority="2">
    <xsl:variable name="elementName" select="local-name()"/>
    <xsl:element name="{$elementName}">
      <xsl:value-of select="."/>
    </xsl:element>
  </xsl:template>

  <xsl:template match="Channel/text()|Category/text()" priority="2">
    <xsl:if test="normalize-space(.)">
      <xsl:element name="Url">
        <xsl:value-of select="."/>
      </xsl:element>
    </xsl:if>
  </xsl:template>

</xsl:stylesheet>

