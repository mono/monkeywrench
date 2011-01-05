<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="Releases"  CodeBehind="Releases.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <script type="text/javascript" src="res/sorttable.js"></script>

	<h2><center>Releases</center></h2>
    <asp:Table runat="server" ID="tblStatus" CssClass="buildstatus releases sortable">
        <asp:TableHeaderRow>
            <asp:TableHeaderCell>Version</asp:TableHeaderCell>
            <asp:TableHeaderCell>Revision</asp:TableHeaderCell>
            <asp:TableHeaderCell>Description</asp:TableHeaderCell>
            <asp:TableHeaderCell>Filename</asp:TableHeaderCell>
            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
        </asp:TableHeaderRow>
    </asp:Table>
    <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
</asp:Content>
