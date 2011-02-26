<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"  >
    <xsl:template match="/">
        <xsl:element name="rdf">
            <xsl:element name="channel">
                <xsl:element name="generator">
                    <xsl:value-of select="//*[name()='channel']/*[name()='generator']"/>
                </xsl:element>
                <xsl:element name="title">
                    <xsl:value-of select="//*[name()='channel']/*[name()='title']"/>
                </xsl:element>
                <xsl:element name="link">
                    <xsl:value-of select="//*[name()='channel']/*[name()='link']"/>
                </xsl:element>
                <xsl:element name="description">
                    <xsl:value-of select="//*[name()='channel']/*[name()='description']"/>
                </xsl:element>
                <xsl:element name="copyright">
                    <xsl:value-of select="//*[name()='channel']/*[name()='copyright']"/>
                </xsl:element>
                <xsl:element name="pubDate">
                    <xsl:value-of select="//*[name()='channel']/*[name()='pubDate']"/>
                </xsl:element>
                <xsl:element name="lastBuildDate">
                    <xsl:value-of select="//*[name()='channel']/*[name()='lastBuildDate']"/>
                </xsl:element>
            </xsl:element>
            <xsl:call-template name="items"/>
        </xsl:element>
    </xsl:template>

    <xsl:template name="items">
        <xsl:for-each select="//*[name()='item']">
            <xsl:element name="item">
                <xsl:element name="title">
                    <xsl:value-of select="child::*[name()='title']"/>
                </xsl:element>
                <xsl:element name="link">
                    <xsl:value-of select="child::*[name()='link']"/>
                </xsl:element>
                <xsl:element name="description">
                    <xsl:value-of select="child::*[name()='description']"/>
                </xsl:element>
                <xsl:element name="pubDate">
                    <xsl:value-of select="child::*[name()='pubDate']"/>
                </xsl:element>
            </xsl:element>
        </xsl:for-each>
    </xsl:template>
</xsl:stylesheet>
