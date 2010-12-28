<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="Identities"
    CodeBehind="Identities.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <asp:Table runat="server" CssClass="center">
        <asp:TableRow>
            <asp:TableCell HorizontalAlign="Center">
                <asp:Table ID="tblIrcIdentities" runat="server" CssClass="buildstatus identity">
                    <asp:TableHeaderRow>
                        <asp:TableHeaderCell ColumnSpan="5">IRC Identities</asp:TableHeaderCell>
                    </asp:TableHeaderRow>
                    <asp:TableHeaderRow>
                        <asp:TableHeaderCell>Name</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Servers</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Channels</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Nicks</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                    </asp:TableHeaderRow>
                    <asp:TableFooterRow>
                        <asp:TableCell>
                            <asp:TextBox ID="txtIrcName" runat="server" ToolTip="&nbsp;"/>
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:TextBox ID="txtIrcServers" runat="server" ToolTip="A comma separated list of irc servers" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:TextBox ID="txtIrcChannels" runat="server" ToolTip="A comma separated list of irc channels to join" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:TextBox ID="txtIrcNicks" runat="server" ToolTip="A comma separated list of irc nicks to use" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:LinkButton ID="lnkIrcAdd" Text="Add" runat="server" OnClick="lnkIrcAdd_Click" />
                        </asp:TableCell>
                    </asp:TableFooterRow>
                </asp:Table>
            </asp:TableCell>
        </asp:TableRow>
        <asp:TableRow>
            <asp:TableCell HorizontalAlign="Center">
                <div id="lblIrcHelp" style="color: Green">&nbsp;</div>
            </asp:TableCell>
        </asp:TableRow>
        <asp:TableRow>
            <asp:TableCell HorizontalAlign="Center">
                <asp:Table ID="tblEmailIdentities" runat="server" CssClass="buildstatus identity">
                    <asp:TableHeaderRow>
                        <asp:TableHeaderCell ColumnSpan="4">Email Identities</asp:TableHeaderCell>
                    </asp:TableHeaderRow>
                    <asp:TableHeaderRow>
                        <asp:TableHeaderCell>Name</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Email</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Password</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                    </asp:TableHeaderRow>
                    <asp:TableFooterRow>
                        <asp:TableCell>
                            <asp:TextBox ID="txtEmailName" runat="server" ToolTip="&nbsp;" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:TextBox ID="txtEmailEmail" runat="server" ToolTip="The email address used to send email" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:TextBox ID="txtEmailPassword" runat="server" ToolTip="The password for the email address" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:LinkButton ID="lnkEmailAdd" Text="Add" runat="server" OnClick="lnkEmailAdd_Click" />
                        </asp:TableCell>
                    </asp:TableFooterRow>
                </asp:Table>
            </asp:TableCell>
        </asp:TableRow>
        <asp:TableRow>
            <asp:TableCell HorizontalAlign="Center">
                <div id="lblEmailHelp" style="color: Green">&nbsp;</div>
            </asp:TableCell>
        </asp:TableRow>
        <asp:TableRow>
            <asp:TableCell HorizontalAlign="Center">
                <asp:Label ID="lblMessage" ForeColor="Red" runat="server" />
            </asp:TableCell>
        </asp:TableRow>
    </asp:Table>
</asp:Content>
