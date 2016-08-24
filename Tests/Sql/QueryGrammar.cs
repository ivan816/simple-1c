using Irony.Parsing;

namespace Simple1C.Tests.Sql
{
    public class QueryGrammar : Grammar
    {
        public NonTerminal JoinChainOpt { get; private set; }
        public NonTerminal IdList { get; private set; }
        public NonTerminal FromClauseOpt { get; private set; }
        public NonTerminal AliasOpt { get; private set; }
        public NonTerminal Id { get; private set; }
        public NonTerminal SelectList { get; private set; }
        public NonTerminal SelectClause { get; private set; }
        public NonTerminal ColumnSource { get; private set; }
        public NonTerminal ColumnItem { get; private set; }
        public NonTerminal ColumnItemList { get; private set; }

        public QueryGrammar()
            : base(false)
        {
            var comment = new CommentTerminal("comment", "/*", "*/");
            var lineComment = new CommentTerminal("line_comment", "--", "\n", "\r\n");
            NonGrammarTerminals.Add(comment);
            NonGrammarTerminals.Add(lineComment);
            var number = new NumberLiteral("number");
            var string_literal = new StringLiteral("string", "'", StringOptions.AllowsDoubledQuote);
            var Id_simple = TerminalFactory.CreateSqlExtIdentifier(this, "id_simple");
            //covers normal identifiers (abc) and quoted id's ([abc d], "abc d")
            var comma = ToTerm(",");
            comma.Flags = TermFlags.IsPunctuation;
            var dot = ToTerm(".");
            var NOT = ToTerm("NOT");
            var ON = ToTerm("ON");
            var SELECT = ToTerm("SELECT");
            var FROM = ToTerm("FROM");
            var AS = ToTerm("AS");
            var COUNT = ToTerm("COUNT");
            var JOIN = ToTerm("JOIN");
            var BY = ToTerm("BY");
            var INTO = ToTerm("INTO");

            //Non-terminals
            Id = new NonTerminal("Id");
            var stmt = new NonTerminal("stmt");
            SelectClause = new NonTerminal("selectStmt");
            IdList = new NonTerminal("idlist");
            var idlistPar = new NonTerminal("idlistPar");
            var orderList = new NonTerminal("orderList");
            var orderMember = new NonTerminal("orderMember");
            var orderDirOpt = new NonTerminal("orderDirOpt");
            var whereClauseOpt = new NonTerminal("whereClauseOpt");
            var expression = new NonTerminal("expression");
            var exprList = new NonTerminal("exprList");
            var selRestrOpt = new NonTerminal("selRestrOpt");
            SelectList = new NonTerminal("selList");
            FromClauseOpt = new NonTerminal("fromClauseOpt");
            var intoClauseOpt = new NonTerminal("intoClauseOpt");
            var groupClauseOpt = new NonTerminal("groupClauseOpt");
            var havingClauseOpt = new NonTerminal("havingClauseOpt");
            var orderClauseOpt = new NonTerminal("orderClauseOpt");
            ColumnItemList = new NonTerminal("columnItemList");
            ColumnItem = new NonTerminal("columnItem");
            ColumnSource = new NonTerminal("columnSource");
            var asOpt = new NonTerminal("asOpt");
            AliasOpt = new NonTerminal("aliasOpt");
            var aggregate = new NonTerminal("aggregate");
            var aggregateArg = new NonTerminal("aggregateArg");
            var aggregateName = new NonTerminal("aggregateName");
            var tuple = new NonTerminal("tuple");
            JoinChainOpt = new NonTerminal("joinChainOpt");
            var joinKindOpt = new NonTerminal("joinKindOpt");
            var term = new NonTerminal("term");
            var unExpr = new NonTerminal("unExpr");
            var unOp = new NonTerminal("unOp");
            var binExpr = new NonTerminal("binExpr");
            var binOp = new NonTerminal("binOp");
            var betweenExpr = new NonTerminal("betweenExpr");
            var parSelectStmt = new NonTerminal("parSelectStmt");
            var notOpt = new NonTerminal("notOpt");
            var funCall = new NonTerminal("funCall");
            var stmtLine = new NonTerminal("stmtLine");
            var semiOpt = new NonTerminal("semiOpt");
            var stmtList = new NonTerminal("stmtList");
            var funArgs = new NonTerminal("funArgs");
            var inStmt = new NonTerminal("inStmt");

            //BNF Rules
            Root = stmtList;
            stmtLine.Rule = stmt + semiOpt;
            semiOpt.Rule = Empty | ";";
            stmtList.Rule = MakePlusRule(stmtList, stmtLine);

            //ID
            Id.Rule = MakePlusRule(Id, dot, Id_simple);

            stmt.Rule = SelectClause;
            idlistPar.Rule = "(" + IdList + ")";
            IdList.Rule = MakePlusRule(IdList, comma, Id);
            orderList.Rule = MakePlusRule(orderList, comma, orderMember);
            orderMember.Rule = Id + orderDirOpt;
            orderDirOpt.Rule = Empty | "ASC" | "DESC";

            SelectClause.Rule = SELECT + selRestrOpt + SelectList + intoClauseOpt + FromClauseOpt + whereClauseOpt +
                                groupClauseOpt + havingClauseOpt + orderClauseOpt;
            selRestrOpt.Rule = Empty | "ALL" | "DISTINCT";
            SelectList.Rule = ColumnItemList | "*";

            ColumnItemList.Rule = MakePlusRule(ColumnItemList, comma, ColumnItem);
            ColumnItem.Rule = ColumnSource + AliasOpt;
            AliasOpt.Rule = Empty | asOpt + Id;
            asOpt.Rule = Empty | AS;
            ColumnSource.Rule = aggregate | Id;
            aggregate.Rule = aggregateName + "(" + aggregateArg + ")";
            aggregateArg.Rule = expression | "*";
            aggregateName.Rule = COUNT | "Avg" | "Min" | "Max" | "StDev" | "StDevP" | "Sum" | "Var" | "VarP";
            intoClauseOpt.Rule = Empty | INTO + Id;
            FromClauseOpt.Rule = Empty | FROM + IdList + JoinChainOpt | FROM + "(" + SelectClause + ")" + Id;
            JoinChainOpt.Rule = Empty | joinKindOpt + JOIN + IdList + ON + Id + "=" + Id;
            joinKindOpt.Rule = Empty | "INNER" | "LEFT" | "RIGHT";
            whereClauseOpt.Rule = Empty | "WHERE" + expression;
            groupClauseOpt.Rule = Empty | "GROUP" + BY + IdList;
            havingClauseOpt.Rule = Empty | "HAVING" + expression;
            orderClauseOpt.Rule = Empty | "ORDER" + BY + orderList;

            //Expression
            exprList.Rule = MakePlusRule(exprList, comma, expression);
            expression.Rule = term | unExpr | binExpr;
            // | betweenExpr; //-- BETWEEN doesn't work - yet; brings a few parsing conflicts 
            term.Rule = Id | string_literal | number | funCall | tuple | parSelectStmt; // | inStmt;
            tuple.Rule = "(" + exprList + ")";
            parSelectStmt.Rule = "(" + SelectClause + ")";
            unExpr.Rule = unOp + term;
            unOp.Rule = NOT | "+" | "-" | "~";
            binExpr.Rule = expression + binOp + expression;
            binOp.Rule = ToTerm("+") | "-" | "*" | "/" | "%" //arithmetic
                         | "&" | "|" | "^" //bit
                         | "=" | ">" | "<" | ">=" | "<=" | "<>" | "!=" | "!<" | "!>"
                         | "AND" | "OR" | "LIKE" | NOT + "LIKE" | "IN" | NOT + "IN";
            betweenExpr.Rule = expression + notOpt + "BETWEEN" + expression + "AND" + expression;
            notOpt.Rule = Empty | NOT;
            //funCall covers some psedo-operators and special forms like ANY(...), SOME(...), ALL(...), EXISTS(...), IN(...)
            funCall.Rule = Id + "(" + funArgs + ")";
            funArgs.Rule = SelectClause | exprList;
            inStmt.Rule = expression + "IN" + "(" + exprList + ")";

            //Operators
            RegisterOperators(10, "*", "/", "%");
            RegisterOperators(9, "+", "-");
            RegisterOperators(8, "=", ">", "<", ">=", "<=", "<>", "!=", "!<", "!>", "LIKE", "IN");
            RegisterOperators(7, "^", "&", "|");
            RegisterOperators(6, NOT);
            RegisterOperators(5, "AND");
            RegisterOperators(4, "OR");

            var commaTerm2 = ToTerm(",");
            commaTerm2.SetFlag(TermFlags.IsPunctuation, false);
            //MarkPunctuation(asOpt, semiOpt);
            //MarkTransient(stmt, term, asOpt, aliasOpt, stmtLine, expression, unOp, tuple);
            binOp.SetFlag(TermFlags.InheritPrecedence);
        }
    }
}