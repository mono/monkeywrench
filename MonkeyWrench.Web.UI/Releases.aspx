<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="Releases"  CodeBehind="Releases.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <asp:Table runat="server" ID="tblStatus" CssClass="buildstatus releases">
        <asp:TableHeaderRow>
            <asp:TableHeaderCell ColumnSpan="5">Releases</asp:TableHeaderCell>
        </asp:TableHeaderRow>
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
