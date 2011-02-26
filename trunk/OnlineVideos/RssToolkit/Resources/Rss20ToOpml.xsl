<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform" >
  <xsl:template match="/">
    <xsl:element name="opml">
      <xsl:element name="head">
        <xsl:attribute name="version">1.1</xsl:attribute>
        <xsl:element name="title">
          <xsl:value-of select="//*[name()='channel']/*[name()='title']"/>
        </xsl:element>
        <xsl:element name="dateCreated">
          <xsl:value-of select="//*[name()='channel']/*[name()='pubDate']"/>
        </xsl:element>
        <xsl:element name="dateModified">
          <xsl:value-of select="//*[name()='channel']/*[name()='lastBuildDate']"/>
        </xsl:element>
        <xsl:element name="ownerEmail">
          <xsl:value-of select="//*[name()='channel']/*[name()='webMaster']"/>
        </xsl:element>
      </xsl:element>
      <xsl:element name="body">
        <xsl:element name="outline">
          <xsl:attribute name="text">
            <xsl:value-of select="//*[name()='channel']/*[name()='title']"/>
          </xsl:attribute>
          <xsl:attribute name="description">
            <xsl:value-of select="//*[name()='channel']/*[name()='description']"/>
          </xsl:attribute>
          <xsl:attribute name="htmlUrl">
            <xsl:value-of select="//*[name()='channel']/*[name()='link']"/>
          </xsl:attribute>
          <xsl:attribute name="title">
            <xsl:value-of select="//*[name()='channel']/*[name()='title']"/>
          </xsl:attribute>
          <xsl:attribute name="type">rss</xsl:attribute>
          <xsl:attribute name="version">2.0</xsl:attribute>
          <xsl:attribute name="xmlUrl">#FillValue#</xsl:attribute>
        </xsl:element>
      </xsl:element>
    </xsl:element>
  </xsl:template>
</xsl:stylesheet>
