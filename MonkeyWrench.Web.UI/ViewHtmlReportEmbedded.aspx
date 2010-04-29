<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="ViewHtmlReportEmbedded" Codebehind="ViewHtmlReportEmbedded.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
		<div id="header" runat="server"></div>
		<div style="width:100%;min-height:100%">
		<iframe id="htmlreport" runat="server" frameborder="0" style="min-height: 100%;width: 100%"></iframe>
		</div>
</asp:Content>
