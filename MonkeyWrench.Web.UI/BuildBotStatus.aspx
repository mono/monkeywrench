<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="BuildBotStatus"
    CodeBehind="BuildBotStatus.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <script type="text/javascript" src="res/sorttable.js"></script>
    <script type="text/javascript" src="BuildBotStatus.js"></script>

	<h2><center>Build bot status</center></h2>
    <asp:Table runat="server" ID="tblStatus" CssClass="buildstatus buildbotstatus sortable">
        <asp:TableHeaderRow>
            <asp:TableHeaderCell>Bot</asp:TableHeaderCell>
            <asp:TableHeaderCell>Last Version</asp:TableHeaderCell>
            <asp:TableHeaderCell>Last Report Date</asp:TableHeaderCell>
            <asp:TableHeaderCell>Configured Version</asp:TableHeaderCell>
        </asp:TableHeaderRow>
    </asp:Table>
    <div><asp:Label ID="lblMessage" runat="server" ForeColor="Red" /></div>
    <asp:Table runat="server" ID="tblReleases" CssClass="buildstatus releases">
        <asp:TableHeaderRow>
            <asp:TableHeaderCell ColumnSpan="4">Releases</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableHeaderRow>
            <asp:TableHeaderCell>Action</asp:TableHeaderCell>
            <asp:TableHeaderCell>Version</asp:TableHeaderCell>
            <asp:TableHeaderCell>Revision</asp:TableHeaderCell>
            <asp:TableHeaderCell>Description</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableRow>
            <asp:TableCell><a href='javascript: executeBuildBotSelection (0)'>Select</a></asp:TableCell>
            <asp:TableCell ColumnSpan="3">Manual</asp:TableCell>
        </asp:TableRow>
    </asp:Table>
</asp:Content>
