<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" Inherits="Users" Codebehind="Users.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="content" runat="Server" EnableViewState="False">
    <div>
        <asp:Table ID="tblUsers" runat="server" CssClass="data index">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell ColumnSpan="5" CssClass="title">
                Users
                <a href='User.aspx' class="aside">Create new user</a>
                </asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>User</asp:TableHeaderCell>
                <asp:TableHeaderCell>Full Name</asp:TableHeaderCell>
                <asp:TableHeaderCell>Roles</asp:TableHeaderCell>
                <asp:TableHeaderCell>Password</asp:TableHeaderCell>
                <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
            </asp:TableHeaderRow>
        </asp:Table>
        <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
    </div>
</asp:Content>
