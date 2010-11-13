<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="xml" indent="yes" cdata-section-elements="item"/>

  <xsl:template match="*" priority="1">
    <xsl:variable name="elementName" select="local-name()"/>
    <xsl:element name="{$elementName}">
      <xsl:apply-templates select="@*|node()"/>
    </xsl:element>
  </xsl:template>

  <xsl:template match="Categories" priority="2">
    <xsl:choose>
      <xsl:when test="node()">
        <xsl:copy>
          <xsl:copy-of select="@*"/>
          <xsl:value-of select="."/>
        </xsl:copy>
      </xsl:when>
    </xsl:choose> 
  </xsl:template>

  <xsl:template match="text()" priority="2">
    <xsl:if test="normalize-space(.)">
      <xsl:value-of select="."/>
    </xsl:if>
  </xsl:template>

  <xsl:template match="@*[local-name() = 'type']" priority="2">
    <xsl:attribute name="xsi:type"><xsl:value-of select="."/></xsl:attribute>
  </xsl:template>  
  
  <xsl:template match="*[local-name() = 'OnlineVideoSites']" priority="2">
    <OnlineVideoSites xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
      <xsl:apply-templates/>
    </OnlineVideoSites>
  </xsl:template>

  <xsl:template match="*[local-name() = 'Site']/*[local-name() != 'Description' and local-name() != 'Configuration' and local-name() != 'Categories']" priority="2">
    <xsl:if test="normalize-space(text())">
      <xsl:variable name="attrName" select="local-name()"/>
      <xsl:attribute name="{$attrName}">
        <xsl:value-of select="text()"/>
      </xsl:attribute>      
    </xsl:if>
  </xsl:template>

  <xsl:template match="*[local-name() = 'Category' or local-name() = 'Channel']/*[local-name() != 'Url' and local-name() != 'Channels' and local-name() != 'SubCategories']" priority="2">
    <xsl:variable name="attrName" select="local-name()"/>
    <xsl:attribute name="{$attrName}">
      <xsl:value-of select="text()"/>
    </xsl:attribute>
  </xsl:template>

  <xsl:template match="*[local-name() = 'item']/@*[local-name() = 'key']" priority="2">
    <xsl:variable name="attrName" select="local-name()"/>
    <xsl:attribute name="{$attrName}">
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>

  <xsl:template match="*[local-name() = 'Category' or local-name() = 'Channel']/*[local-name() = 'Url']" priority="2">
    <xsl:value-of select="."/>
  </xsl:template> 
  
</xsl:stylesheet>

