<?xml version="1.0" ?>
<xsl:stylesheet version="1.1" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:media="http://search.yahoo.com/mrss/" xmlns:opensearch="http://a9.com/-/spec/opensearch/1.1/">
  <xml:output mode="xml" encoding="UTF-8"/>

  <xsl:template match="/">
    <rss version="2.0" xmlns:media="http://search.yahoo.com/mrss/" xmlns:opensearch="http://a9.com/-/spec/opensearch/1.1/">
      <xsl:element name="channel">
        <xsl:element name="generator">
          <xsl:value-of select="//*[name()='generator']"/>
        </xsl:element>
        <xsl:element name="title">
          <xsl:value-of select="//*[name()='title']"/>
        </xsl:element>
        <xsl:element name="link">
          <xsl:value-of select="//*[name()='link']/@href"/>
        </xsl:element>
        <xsl:element name="description">
          <xsl:value-of select="//*[name()='tagline']"/>
        </xsl:element>
        <xsl:element name="copyright">
          <xsl:value-of select="//*[name()='copyright']"/>
        </xsl:element>
        <xsl:element name="pubDate">
          <xsl:value-of select="//*[name()='modified']"/>
        </xsl:element>
        <xsl:element name="lastBuildDate">
          <xsl:value-of select="//*[name()='modified']"/>
        </xsl:element>
        <xsl:apply-templates select="//opensearch:*"/>
        <xsl:call-template name="items"/>
      </xsl:element>
    </rss>
  </xsl:template>


  <xsl:template name="items">
    <xsl:for-each select="//*[name()='entry']">
      <xsl:element name="item">
        <xsl:element name="title">
          <xsl:value-of select="child::*[name()='title']"/>
        </xsl:element>
        <xsl:element name="link">
          <xsl:value-of select="child::*[name()='link']/@href"/>
        </xsl:element>
        <xsl:element name="guid">
          <xsl:value-of select="child::*[name()='id']"/>
        </xsl:element>
        <xsl:element name="category">
          <xsl:value-of select="child::*[name()='summary']"/>
        </xsl:element>
        <xsl:element name="description">
          <xsl:value-of select="child::*[name()='content']"/>
        </xsl:element>
        <xsl:element name="pubDate">
          <xsl:value-of select="child::*[name()='modified' or name()='updated']"/>
        </xsl:element>
        <xsl:apply-templates select="media:*"/>
        <xsl:apply-templates select="*/media:*"/>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>

  
  <xsl:template match="media:*|opensearch:*">
    <xsl:element name="{name()}" namespace="{namespace-uri()}">
      <xsl:copy-of select="@*"/>
      <xsl:apply-templates/>
    </xsl:element>
  </xsl:template>

</xsl:stylesheet>
