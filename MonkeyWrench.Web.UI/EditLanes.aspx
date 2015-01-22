<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="EditLanes" Codebehind="EditLanes.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <script type="text/javascript" src="EditLanes.js"></script>
    <asp:Table ID="tblLanes" runat="server" CssClass="data index">
        <asp:TableHeaderRow>
            <asp:TableHeaderCell ColumnSpan="2" CssClass="title">Lanes</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableHeaderRow>
            <asp:TableHeaderCell>Name</asp:TableHeaderCell>
            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableRow>
            <asp:TableCell><input type='text' value='lane' name='txtLane' id='txtLane' /></asp:TableCell>
            <asp:TableCell><a href='javascript:addLane ()'>Add</a></asp:TableCell>
        </asp:TableRow>
    </asp:Table>
    <div><asp:Label ID="lblMessage" runat="server" ForeColor="Red"/></div>
</asp:Content>
