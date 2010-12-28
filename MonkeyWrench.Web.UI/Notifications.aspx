<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="Notifications"
    CodeBehind="Notifications.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server" EnableViewState="false">
    <asp:Table runat="server" CssClass="center">
        <asp:TableRow>
            <asp:TableCell HorizontalAlign="Center">
                <asp:Table ID="tblNotifications" runat="server" CssClass="buildstatus notifications">
                    <asp:TableHeaderRow>
                        <asp:TableHeaderCell ColumnSpan="6">Notifications</asp:TableHeaderCell>
                    </asp:TableHeaderRow>
                    <asp:TableHeaderRow>
                        <asp:TableHeaderCell>Name</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Identity</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Identity Type</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Mode</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Notification Type</asp:TableHeaderCell>
                        <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                    </asp:TableHeaderRow>
                    <asp:TableFooterRow>
                        <asp:TableCell>
                            <asp:TextBox ID="txtName" runat="server" ToolTip="&nbsp;" />
                        </asp:TableCell>
                        <asp:TableCell ColumnSpan="2">
                            <asp:DropDownList ID="cmbIdentity" runat="server" ToolTip="The identity to use for this notification" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:DropDownList ID="cmbMode" runat="server" ToolTip="Notification mode. 'Default' will just report the failure to the committer. 
'MoonlightDrt' and 'NUnit' will try to find the corresponding xml file with test results, 
and report to test owner (if known) or committer with more information about which tests failed.">
                                <asp:ListItem Text="Default" Value="0" />
                                <asp:ListItem Text="MoonlightDrt" Value="1" />
                                <asp:ListItem Text="NUnit" Value="2" />
                            </asp:DropDownList>
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:DropDownList ID="cmbNotificationType" runat="server" ToolTip="Notification type">
                                <asp:ListItem Text="Fatal failures only" Value="0" />
                                <asp:ListItem Text="Nonfatal failures only" Value="1" />
                                <asp:ListItem Text="All failures" Value="2" />
                            </asp:DropDownList>
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:LinkButton ID="lnkAdd" Text="Add" runat="server" OnClick="lnkAdd_Click" />
                        </asp:TableCell>
                    </asp:TableFooterRow>
                </asp:Table>
            </asp:TableCell>
        </asp:TableRow>
        <asp:TableRow>
            <asp:TableCell HorizontalAlign="Center">
                <div id="lblHelp" style="color: Green">&nbsp;</div>
            </asp:TableCell>
        </asp:TableRow>
        <asp:TableRow>
            <asp:TableCell HorizontalAlign="Center">
                <asp:Label ID="lblMessage" ForeColor="Red" runat="server" />
            </asp:TableCell></asp:TableRow>
    </asp:Table>
</asp:Content>
