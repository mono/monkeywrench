<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" Inherits="Users" Codebehind="Users.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="content" runat="Server" EnableViewState="False">
    <div>
        <asp:Table ID="tblUsers" runat="server" CssClass="buildstatus users">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell ColumnSpan="5">Users</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>User</asp:TableHeaderCell>
                <asp:TableHeaderCell>FullName</asp:TableHeaderCell>
                <asp:TableHeaderCell>Roles</asp:TableHeaderCell>
                <asp:TableHeaderCell>Password</asp:TableHeaderCell>
                <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableRow>
                <asp:TableCell ColumnSpan="5"><a href='User.aspx'>Create new user</a></asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
    </div>
</asp:Content>
