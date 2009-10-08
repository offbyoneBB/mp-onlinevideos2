<?xml version="1.0" ?>
<xsl:stylesheet version="1.1" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xml:output mode="xml" encoding="UTF-8"/>

  <xsl:template match="/">
    <xsl:element name="feed">
      <xsl:element name="generator">
        <xsl:value-of select="//*[name()='generator']"/>
      </xsl:element>
      <xsl:element name="title">
        <xsl:value-of select="//*[name()='title']"/>
      </xsl:element>
      <xsl:element name="link">
        <xsl:attribute name="href">
          <xsl:value-of select="//*[name()='link']"/>
        </xsl:attribute>
        <xsl:attribute name="rel">alternate</xsl:attribute>
        <xsl:attribute name="type">text/html"</xsl:attribute>
      </xsl:element>
      <xsl:element name="tagline">
        <xsl:value-of select="//*[name()='description']"/>
      </xsl:element>
      <xsl:element name="copyright">
        <xsl:value-of select="//*[name()='copyright']"/>
      </xsl:element>
      <xsl:element name="modified">
        <xsl:value-of select="//*[name()='pubDate']"/>
      </xsl:element>
      <xsl:call-template name="entry"/>
    </xsl:element>
  </xsl:template>


  <xsl:template name="entry">
    <xsl:for-each select="//*[name()='channel']/*[name()='item']">
      <xsl:element name="entry">
        <xsl:element name="title">
          <xsl:value-of select="child::*[name()='title']"/>
        </xsl:element>
        <xsl:element name="link">
          <xsl:attribute name="href">
            <xsl:value-of select="child::*[name()='link']"/>
          </xsl:attribute>
          <xsl:attribute name="rel">alternate</xsl:attribute>
          <xsl:attribute name="type">text/html"</xsl:attribute>
        </xsl:element>
        <xsl:element name="guid">
          <xsl:value-of select="child::*[name()='id']"/>
        </xsl:element>
        <xsl:element name="summary">
          <xsl:value-of select="child::*[name()='category']"/>
        </xsl:element>
        <xsl:element name="content">
          <xsl:attribute name="type">text/html</xsl:attribute>
          <xsl:value-of select="child::*[name()='description']"/>
        </xsl:element>
        <xsl:element name="modified">
          <xsl:value-of select="child::*[name()='pubDate']"/>
        </xsl:element>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>
</xsl:stylesheet>
