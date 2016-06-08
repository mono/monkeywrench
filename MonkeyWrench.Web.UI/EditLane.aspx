<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="EditLane" CodeBehind="EditLane.aspx.cs" %>
<%@ Register Src="~/EnvironmentVariablesEditor.ascx"  TagName="EnvironmentVariablesEditor" TagPrefix="ucs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">

    <script type="text/javascript" src="EditLane.js"></script>
    <script type="text/javascript" src="MonkeyWrench.js"></script>

    <h2>
        <asp:Label ID="lblH2" runat="server"></asp:Label></h2>
    <div>
        <asp:Table runat="server">
            <asp:TableRow>
                <asp:TableCell>Id:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtID" runat="server" Width="600px">0</asp:TextBox></asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Lane:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtLane" runat="server" Width="600px"></asp:TextBox></asp:TableCell><asp:TableCell>The name of this lane. Must be unique on this server.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Source control:</asp:TableCell>
                <asp:TableCell>
                    <asp:DropDownList ID="cmbSourceControl" runat="server" Width="600px">
                        <asp:ListItem Text="git" Value="git" />
                    </asp:DropDownList>
                </asp:TableCell>
                <asp:TableCell>Source control.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Repository:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtRepository" runat="server" Width="600px"></asp:TextBox></asp:TableCell><asp:TableCell>The repository where the code for this lane is located. The scheduler uses the revisions for this repository to schedule work. Can include multiple repositories separated with commas.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Commit filter:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtCommitFilter" runat="server" Width="600px"></asp:TextBox></asp:TableCell>
                    <asp:TableCell>A filter to filter out commits. An empty filter means include all commits. If not empty, the filter must start with either 'IncludeAllExcept:' or 'ExcludeAllExcept:' and followed by a semi-colon separated list of shell globs.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Min revision:</asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtMinRevision" runat="server" Width="600px"></asp:TextBox></asp:TableCell><asp:TableCell>Revisions before this one won't get scheduled. Leave blank to include all revisions.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Max revision:</asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtMaxRevision" runat="server" Width="600px"></asp:TextBox>
                </asp:TableCell>
                <asp:TableCell>Revisions after this one won't get scheduled. Leave blank to include all (future) revisions. Git: If you want to track a branch other than the 'remotes/origin/master', set the revision to the remote branch you want to track (you need one max revision per repository in this case). The value '--all' can be used to schedule for any repository branch</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Tags:</asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtTags" runat="server" Width="600px"></asp:TextBox>
                </asp:TableCell>
                <asp:TableCell>Comma-separated list of tags for this lane. Descendant lanes do not inherit tags. Tags can contain any character except commas (in particular spaces are allowed).</asp:TableCell>
            </asp:TableRow>
			<asp:TableRow>
                <asp:TableCell>Required Roles:</asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtRoles" runat="server" Width="600px"></asp:TextBox>
                </asp:TableCell>
                <asp:TableCell>Comma-separated list of roles that can edit this lane.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Parent lane:</asp:TableCell>
                <asp:TableCell>
                    <asp:DropDownList ID="lstParentLane" runat="server" Width="600px"></asp:DropDownList>
                </asp:TableCell>
                <asp:TableCell>The (optional) parent lane of this lane.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Child lanes:</asp:TableCell>
                <asp:TableCell>
                    <asp:Label ID="lblChildLanes" runat="server" Width="600px"></asp:Label>
                </asp:TableCell>
                <asp:TableCell></asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Traverse merges:</asp:TableCell>
                <asp:TableCell>
                    <asp:CheckBox ID="chkTraverseMerges" runat="server" Width="600px"></asp:CheckBox>
                </asp:TableCell>
                <asp:TableCell>If merged-in commits (except the merge commit itself, which is always included) should be included.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Enabled:</asp:TableCell>
                <asp:TableCell>
                    <asp:CheckBox ID="chkEnabled" runat="server" Width="600px"></asp:CheckBox>
                </asp:TableCell>
                <asp:TableCell>If a lane is enabled. If not enabled nothing will be done for it and it won't show up on the front page.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                </asp:TableCell>
                <asp:TableCell>
                    <asp:Button ID="cmdSave" runat="server" Text="Save" OnClick="cmdSave_Click" />
                    <asp:Button ID="cmdDeleteAllWork" runat="server" Text="Delete all work" OnClick="cmdDeleteAllWork_Click" />
                    <asp:Button ID="cmdClearAllWork" runat="server" Text="Clear all work" OnClick="cmdClearAllWork_Click" />
                    <asp:Button ID="cmdDeleteAllRevisions" runat="server" Text="Delete all revisions" OnClick="cmdDeleteAllRevisions_Click" />
                    <asp:Button id="cmdDontDoWork" runat="server" Text="Mark all pending revisions as 'Don't build'" OnClick="cmdDontDoWork_Click" />
                 </asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <asp:Table runat="server">
            <asp:TableRow runat="server">
                <asp:TableCell>
                    <asp:Table runat="server" ID="tblDependencies" CssClass="commands">
                        <asp:TableHeaderRow CssClass="commands_header">
                            <asp:TableHeaderCell ColumnSpan="6">Dependencies</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell>Dependent lane</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Condition</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Host</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Filename</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Files to download</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                    </asp:Table>
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow runat="server">
                <asp:TableCell>
                    <asp:Table runat="server" ID="tblCommands" CssClass="commands">
                        <asp:TableHeaderRow CssClass="commands_header">
                            <asp:TableHeaderCell ColumnSpan="11">Commands</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell>Sequence</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Command</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Always Execute</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Non fatal</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Internal</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Executable</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Arguments</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Timeout</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Deadlock Timeout</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Working Directory</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Upload files</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Timestamp</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Notes</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                    </asp:Table>
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow runat="server">
                <asp:TableCell>
                    <asp:Table runat="server" ID="tblFiles" CssClass="commands">
                        <asp:TableHeaderRow CssClass="commands_header">
                            <asp:TableHeaderCell ColumnSpan="4">Files</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell>Filename</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Mime</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Used in</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                    </asp:Table>
                </asp:TableCell>
            </asp:TableRow>
<%--
            <asp:TableRow runat="server">
                <asp:TableCell>
                    <asp:Table runat="server" ID="tblDeletionDirective" CssClass="commands">
                        <asp:TableHeaderRow CssClass="commands_header">
                            <asp:TableHeaderCell ColumnSpan="7"><a href="doc/EditLaneHelp.html#id_DeletionDirectives">Retention directives</a></asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell>Directive</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Filename</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Glob mode</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Condition</asp:TableHeaderCell>
                            <asp:TableHeaderCell>X</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Enabled</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                        <asp:TableRow>
                            <asp:TableCell>
                                <asp:TextBox ID="txtDeletionDirective1" runat="server" Text="description" />
                            </asp:TableCell>
                            <asp:TableCell>
                                <asp:TextBox ID="txtDeletionDirectiveFilename1" Text="filename" runat="server" />
                            </asp:TableCell>
                            <asp:TableCell>
                                <asp:DropDownList ID="lstDeletionDirectiveGlobs1" runat="server" />
                            </asp:TableCell>
                            <asp:TableCell>
                                <asp:DropDownList ID="lstDeletionDirectiveCondition1" runat="server" />
                            </asp:TableCell>
                            <asp:TableCell>
                                <asp:TextBox ID="txtDeletionDirectiveX1" runat="server" />
                            </asp:TableCell>
                            <asp:TableCell>
                                <asp:CheckBox ID="chkDeletionDirectiveEnabled1" runat="server" />
                            </asp:TableCell>
                            <asp:TableCell>
                                <asp:LinkButton ID="lnkAddDeletionDirective1" CommandName="addDeletionDirective" CommandArgument="1" OnCommand="btn_Command" runat="server" Text="Add" />
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow id="rowDeletionDirectives2" runat="server">
                            <asp:TableCell>
                                <asp:DropDownList ID="lstDeletionDirectives2" runat="server" />
                            </asp:TableCell>
                            <asp:TableCell>
                                -
                            </asp:TableCell>
                            <asp:TableCell>
                                -
                            </asp:TableCell>
                            <asp:TableCell>
                                -
                            </asp:TableCell>
                            <asp:TableCell>
                                -
                            </asp:TableCell>
                            <asp:TableCell>
                                <asp:CheckBox ID="chkDeletionDirectiveEnabled2" runat="server" />
                            </asp:TableCell>
                            <asp:TableCell>
                                <asp:LinkButton ID="lnkAddDeletionDirective2" CommandName="addDeletionDirective" CommandArgument="2" OnCommand="btn_Command" runat="server" Text="Link" />
                            </asp:TableCell>
                        </asp:TableRow>
                    </asp:Table>
                    <asp:Label Visible="false" ID="lblDeletionDirectiveErrors" runat="server" ForeColor="Red" />
                </asp:TableCell>
            </asp:TableRow>
--%>
            <asp:TableRow runat="server">
                <asp:TableCell>
                    <asp:Table runat="server" ID="tblHosts" CssClass="commands">
                        <asp:TableHeaderRow CssClass="commands_header">
                            <asp:TableHeaderCell ColumnSpan="2">Hosts</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell>Host</asp:TableHeaderCell>
                            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                    </asp:Table>
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow runat="server">
                <asp:TableCell>
                    <asp:Table runat="server" CssClass="commands">
                        <asp:TableHeaderRow CssClass="commands_header">
                            <asp:TableHeaderCell>Environment Variables</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                        <asp:TableRow>
                            <asp:TableCell>
                                <ucs:EnvironmentVariablesEditor ID="editorVariables" runat="server"></ucs:EnvironmentVariablesEditor>
                            </asp:TableCell>
                        </asp:TableRow>
                    </asp:Table>
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow runat="server">
                <asp:TableCell>
                    <asp:Table ID="tblNotifications" runat="server"  CssClass="commands">
                        <asp:TableHeaderRow CssClass="commands_header">
                            <asp:TableHeaderCell ColumnSpan="2">Notifications</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell>Name</asp:TableHeaderCell><asp:TableHeaderCell>Type</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                        <asp:TableFooterRow>
                            <asp:TableCell>
                                <asp:DropDownList ID="cmbNotifications" runat="server" /> 
                            </asp:TableCell>
                            <asp:TableCell>
                                <asp:LinkButton ID="lnkAddNotification" runat="server" Text="Add" OnClick="lnkAddNotification_Click" />
                            </asp:TableCell>
                        </asp:TableFooterRow>
                    </asp:Table>
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow runat="server">
                <asp:TableCell>
                    <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
                </asp:TableCell>
            </asp:TableRow>
        </asp:Table>
    </div>
</asp:Content>
