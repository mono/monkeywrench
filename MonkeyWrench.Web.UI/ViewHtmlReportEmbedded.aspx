<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="ViewHtmlReportEmbedded" Codebehind="ViewHtmlReportEmbedded.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
	<div id="header" class="embeddedheader" runat="server"></div>
	<iframe id="htmlreport" runat="server" frameborder="0" marginheight="0"></iframe>
</asp:Content>
