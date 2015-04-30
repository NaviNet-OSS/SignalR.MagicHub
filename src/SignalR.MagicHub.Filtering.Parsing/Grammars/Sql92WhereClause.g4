grammar Sql92WhereClause;


parse
 : expr EOF
 ;

/*
    SQLite understands the following binary operators, in order from highest to
    lowest precedence:

    ||
    *    /    %
    +    -
    <<   >>   &    |
    <    <=   >    >=
    =    ==   !=   <>   IS   IS NOT   IN   LIKE   GLOB   MATCH   REGEXP
    AND
    OR
*/
expr
 : literal_value																	# constExpr
 | column_name																		# columnExpr
 //| unary_operator expr																# unaryExpr
 //| expr '||' expr
 //| expr ( '*' | '/' | '%' ) expr
 //| expr ( '+' | '-' ) expr
 //| expr ( '<<' | '>>' | '&' | '|' ) expr
 | lhs=expr op=BINARY_OPERATOR rhs=expr												# binaryExpr
 | lhs=expr op=CONJUNCTION_OPERATOR rhs=expr										# binaryExpr
 //| expr K_AND expr
 //| expr K_OR expr
 //| function_name '(' ( K_DISTINCT? expr ( ',' expr )* | '*' )? ')'
 | '(' expr ')'																		# parenExpr
 //| expr K_NOT? ( K_LIKE | K_GLOB | K_REGEXP | K_MATCH ) expr ( K_ESCAPE expr )?
 //| nullexpr=expr K_IS ( K_NOTNULL | K_NOT K_NULL )											# isNullExpr
 //| expr K_IS K_NOT? expr
 | val=expr K_NOT? K_BETWEEN lbound=expr K_AND rbound=expr							# isBetweenExpr
 ;



 SIGNED_NUMBER
 : ( '+' | '-' )? NUMERIC_LITERAL;
// todo: include signed_number instead of parsing -5 as (expr (unary_operator -) (expr (literal_value 5)))
literal_value
 : SIGNED_NUMBER
 | stringLiteral
 //| BLOB_LITERAL
 | K_NULL
 //| K_CURRENT_TIME
 //| K_CURRENT_DATE
 //| K_CURRENT_TIMESTAMP
 ;

//unary_operator
// : '-'
// | '+'
// | '~'
// | K_NOT
// ;


// TODO check all names below

//name
// : any_name
// ;

//function_name
// : any_name
// ;


column_name 
 : any_name
 ;



any_name
 : IDENTIFIER 
 | stringLiteral
 | '(' any_name ')'
 ;

 CONJUNCTION_OPERATOR
  : K_AND
  | K_OR
  ;
 BINARY_OPERATOR
  : LT
  | GT
  | LT_EQ
  | GT_EQ
  | ASSIGN
  | NOT_EQ1
  | NOT_EQ2
  ;
SCOL : ';';
DOT : '.';
OPEN_PAR : '(';
CLOSE_PAR : ')';
COMMA : ',';
ASSIGN : '=';
STAR : '*';
PLUS : '+';
MINUS : '-';
TILDE : '~';
PIPE2 : '||';
DIV : '/';
MOD : '%';
LT2 : '<<';
GT2 : '>>';
AMP : '&';
PIPE : '|';
LT : '<';
LT_EQ : '<=';
GT : '>';
GT_EQ : '>=';
EQ : '==';
NOT_EQ1 : '!=';
NOT_EQ2 : '<>';

// http://www.sqlite.org/lang_keywords.html
K_AND : 'AND';
K_BETWEEN : 'BETWEEN';
//K_CURRENT_DATE : 'CURRENT_DATE';
//K_CURRENT_TIME : 'CURRENT_TIME';
//K_CURRENT_TIMESTAMP : 'CURRENT_TIMESTAMP';
//K_GLOB : 'GLOB';
K_IN : 'IN';
K_IS : 'IS';
K_ISNULL : 'ISNULL';
K_LIKE : 'LIKE';
K_MATCH : 'MATCH';
K_NO : 'NO';
K_NOT : 'NOT';
K_NOTNULL : 'NOTNULL';
K_NULL : 'NULL';
K_OR : 'OR';
//K_REGEXP : 'REGEXP';


IDENTIFIER
 : '"' (~'"' | '""')* '"'
 | '`' (~'`' | '``')* '`'
 | '[' ~']'* ']'
 | [a-zA-Z_] [a-zA-Z_0-9]* // TODO check: needs more chars in set
 ;

NUMERIC_LITERAL
 : DIGIT+ ( '.' DIGIT* )? ( 'E' [-+]? DIGIT+ )?
 | '.' DIGIT+ ( 'E' [-+]? DIGIT+ )? 
 ;


//BLOB_LITERAL
// : X STRING_LITERAL
// ;

SPACES
 : [ \u000B\t\r\n] -> channel(HIDDEN)
 ;

UNEXPECTED_CHAR
 : .
 ;

fragment DIGIT : [0-9];

//fragment A : [aA];
//fragment B : [bB];
//fragment C : [cC];
//fragment D : [dD];
//fragment E : [eE];
//fragment F : [fF];
//fragment G : [gG];
//fragment H : [hH];
//fragment I : [iI];
//fragment J : [jJ];
//fragment K : [kK];
//fragment L : [lL];
//fragment M : [mM];
//fragment N : [nN];
//fragment O : [oO];
//fragment P : [pP];
//fragment Q : [qQ];
//fragment R : [rR];
//fragment S : [sS];
//fragment T : [tT];
//fragment U : [uU];
//fragment V : [vV];
//fragment W : [wW];
//fragment X : [xX];
//fragment Y : [yY];
//fragment Z : [zZ];



fragment BEGIN_STRING
  : '\'' 
  ;


//STRING_LITERAL_TEXT
//    : ~'\''
//    ;
//fragment HEX_DIGIT
//  : [a-fA-F0-9]
//  ;

//STRING_LITERAL_UNICODE_ESCAPE
//    : '\\u' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT
//    ;

//STRING_LITERAL_BASIC_ESCAPE
//    : '\'\'' 
//    ;

//STRING_LITERAL_INVALID_ESCAPE
//    : '\\u' (HEX_DIGIT (HEX_DIGIT HEX_DIGIT?)?)?
//    | '\\' .
//    ;

//END_STRING
//    : '\'' 
//    ;


//STRING_LITERAL2
//: BEGIN_STRING
//    ( STRING_LITERAL_TEXT
//    | STRING_LITERAL_UNICODE_ESCAPE
//    | STRING_LITERAL_BASIC_ESCAPE
//    | STRING_LITERAL_INVALID_ESCAPE
//    )*
//    (END_STRING )
//  ;

stringBody : ( ~'\'' | '\'\'' )*;
stringLiteral
  : '\'' body=stringBody '\''
  ;

 // STRING_LITERAL
 //: '\'' ( ~'\'' | '\'\'' )* '\''
 //;
