Imports System.Math
Imports Microsoft.VisualBasic

Public Class cFormulas
    '--------------------------------------------------------------------------
    ' cFormulas                                                         (06/Feb/99)
    '   Clase para evaluar expresiones

    '--------------------------------------------------------------------------
    ' Revisi�n de  7/Feb/99 Ahora se evaluan correctamente los operadores con m�s
    '                       precedencia, en este orden: % ^ * / \ + -
    '                       Tambi�n se permiten par�ntesis no balanceados,
    '                       al menos los de apertura.
    '                       Si faltan los de cierre se a�aden al final.
    '                       Se permiten definir Funciones y usar algunas internas
    '                       siempre que no necesiten par�metros (por ahora Rnd)
    ' Revisi�n de  9/Feb/99 Se pueden usar funciones internas con par�metros:
    '                       Por ahora:
    '                       Int, Fix, Abs, Sgn, Sqr, Cos, Sin, Tan, Atn, Exp, Log
    ' Revisi�n de  9/Feb/99 Se permiten funciones con un n�mero variable de par�metros
    '                       aunque las operaciones a realizar en los par�metros
    '                       opcionales siempre sea la misma... algo es algo
    '                       Se eval�a la expresi�n por si hay asignaciones a variables
    '                       y de ser as�, se crean esas variables y despu�s se eval�a
    '                       el resto de la expresi�n.
    '                       Esto permite crear variables en la misma cadena a evaluar,
    '                       sin tener que asignarlas antes.
    '                       Este ejemplo devolver� el valor mayor de X o Y
    '                       X = A+B : Y = C-X : Max(x,y)
    '                       Las asignaciones NO se pueden hacer as�:
    '                       x:=10:y:=20:x+y (devolver�a 0)
    '                       Tambi�n permite cadenas de caracteres, aunque lo que haya
    '                       entre comillas simplemente se repetir�:
    '                       x=10:y=20:"El mayor de x e y =";Max(x,y)
    '                       Si se hace esto otro:
    '                       x=10:y=20:s="El mayor de x e y =";Max(x,y):s
    '                       Devolver�:"El mayor de x e y ="20
    '                       Ya que se eval�a como s=Max(x,y)
    '                       Esto otro:
    '                       x=10:y=20:s="El mayor de x e y =";z:z=Max(x,y)
    '                       Mostrar�:
    '                       "El mayor de x e y ="
    '                       Ya que se asigna s=z, pero no se muestra el valor de z
    '                       RESUMIENDO:
    '                       Se pueden usar cadenas entre comillas pero no se pueden
    '                       asignar a variables... (al menos por ahora)
    '                       Se puede incrementar una variable en una asignaci�n
    '                       x=10:x=x+5+x ser�a igual a 10+5+10 = 25
    '                       x=10:x=x+1 ser�a igual a 10+1 = 11
    ' Revisi�n de 10/Feb/99 Algunas correcciones de las cadenas y otras cosillas
    '                       Cuando se asigna un valor a una variable existente
    '                       M�todo para recuperar la f�rmula de una funci�n
    ' Revisi�n de 11/Feb/99 Funci�n de Redondeo
    ' Revisi�n de 12/Feb/99 Nuevas funciones de la clase y definidas y otras mejoras
    ' Revisi�n de 13/Feb/99 Comprobaci�n de n�meros con notaci�n cient�fica
    ' Revisi�n de 14/Feb/99 Acepta hasta 100 par�metros
    ' Revisi�n de 11/Ene/01 Evaluar correctamente la precedencia en los c�lculos
    ' Revisi�n de 22/Ene/01 Arreglado nuevo bug en Calcular
    ' Revisi�n de 28/Ene/01 Cambio en la forma de calcular los n�meros,
    '                       los almaceno en Variant para hacer los c�lculos con Cdec(
    '                       ya que fallaba con n�meros de notaci�n cient�fica
    ' Revisi�n de 29/Ene/01 Propiedad para devolver un valor con notaci�n cient�fica
    '                       o decimal, para el caso de valores muy grandes o peque�os
    ' Revisi�n de 22/Feb/01 Fallaba en c�lculos simples como: 3*2+5
    ' Revisi�n de 29/Oct/02 Arreglo al realizar Instr con una cadena vac�a
    ' Revisi�n de 02/Nov/02 Nuevas funciones Hex, Oct, Round (usando la funci�n de VB)
    '                       Bin, Bin2Dec, Dec2Bin
    '                       No usar notaci�n cient�fica con las funciones Bin...
    '             03/Nov/02 No recalcular las funciones internas, (ver esFunVB)
    '                       Nuevas funciones: Ln, (es igual que Log), Log10, LogX,
    '                       Hex2Dec, Oct2Dec, Dec2Hex (=Hex), Dec2Oct (=Oct)
    '                       Las f�rmulas de las funciones internas prevalecen
    '                       sobre los cambios hechos externamente.
    '                       Declaro PI para efectuar la conversi�n de grados
    '                       a radianes y viceversa (Grados2Radianes, Radianes2Grados)
    '                       Arreglado bug cuando la expresi�n est� entre par�ntesis
    '
    '--------------------------------------------------------------------------
    ' Esta es una nueva implementaci�n del m�dulo Formula.bas y la clase cEvalOp
    ' Aunque los m�todos usados son totalmente diferentes y realmente no es una
    ' mejora, est�n basados en dichos m�dulos... o casi...
    '

    '--------------------------------------------------------------------------
    'Option Explicit
    'Option Compare Text

    Private mNumFunctions As Long       ' El n�mero de funciones "propias"
    Private esFunVB As Boolean
    Private m_NotacionCientifica As Boolean
    '
    ' Funciones Internas soportadas en el programa,
    ' debe indicarse el par�ntesis y un espacio de separaci�n
    'Const FunVBNum As String = "Int( Fix( Abs( Sgn( Sqr( Cos( Sin( Tan( Atn( Exp( Log( Iif( "
    ' Bin( no es una funci�n de VB, pero se usar� como si fuera...      (02/Nov/02)
    Const FunVBNum As String = "Int( Fix( Abs( Sgn( Sqr( Cos( Sin( Tan( Atn( Exp( Log( Ln( Log10( Round( Hex( Dec2Hex( Oct( Dec2Oct( Bin2Dec( Hex2Dec( Oct2Dec( "
    ' S�mbolos a usar para separar los Tokens
    Private Simbols As String
    ' Signos a usar para comentarios
    Private RemSimbs As String


    Public Class tVariable
        Public Name As String
        Public Value As String
    End Class
    ' Array de variables
    Private aVariables(-1) As tVariable


    Public Class tFunctions
        Public Name As String
        Public Params As String
        Public Formula As String
        'Descripcion As String

        Protected Overrides Sub Finalize()
            MyBase.Finalize()
        End Sub
    End Class
    ' Array de funciones
    Private aFunctions(-1) As tFunctions

    Public Function MTrim(ByVal sVar As String, Optional ByVal NoEval As String = "") As String
        '--------------------------------------------------------------------------
        ' Quita todos los espacios y blancos del par�metro pasado           (09/Feb/99)
        ' Los par�metros:
        '   sVar    Cadena a la que se quitar�n los blancos
        '   NoEval  Si se especifica, pareja de caracteres que encerrar�n una cadena
        '           a la que no habr� que quitar los espacios y blancos
        '--------------------------------------------------------------------------
        Dim i As Long
        Dim j As Long
        Dim sTmp As String
        Dim sBlancos As String

        ' Se entienden como blancos: espacios, Tabs y Chr$(0)
        sBlancos = " " & vbTab & Chr(0)
        ' NoEval tendr� el caracter que no se evaluar� para quitar espacios
        ' por ejemplo si no queremos quitar los caracteres entre comillas
        ' NoEval ser� chr$(34), e ir� por pares o hasta el final de la cadena

        sTmp = ""
        For i = 1 To Len(sVar)
            ' Si es el caracter a no evaluar
            If Mid$(sVar, i, 1) = NoEval Then
                ' Buscar el siguiente caracter
                j = InStr(i + 1, sVar, NoEval, CompareMethod.Text)
                If j = 0 Then
                    sVar = sVar & NoEval
                    j = Len(sVar)
                End If
                sTmp = sTmp & Mid$(sVar, i, j - i + 1)
                i = j '+ 1
                ' Si no es uno de los caracteres "blancos"
            ElseIf InStr(sBlancos, Mid$(sVar, i, 1)) = 0 Then
                ' Asignarlo a la variable final
                sTmp = sTmp & Mid$(sVar, i, 1)
            End If
        Next
        MTrim = sTmp
    End Function

    Public Function AsignarVariables(ByVal v As String, _
                                     Optional ByVal NoEval As String = "") As String
        '--------------------------------------------------------------------------
        ' Asignar las variables, si las hay                                 (09/Feb/99)
        ' Los par�metros de entrada:
        '   v       Expresi�n con posibles asignaciones
        '   NoEval  Si se especifica, pareja de caracteres que encerrar�n una cadena
        '           en la que no se buscar�n variables
        '
        ' Devolver� el resto de la cadena que ser� la expresi�n a evaluar
        '--------------------------------------------------------------------------
        Dim i As Long
        Dim j As Long
        Dim k As Long
        Dim sNom As String
        Dim sVal As String
        Dim sExpr As String
        Dim sValAnt As String
        Dim sOp As String

        ' Quitar todos los espacios, excepto los que est�n entre comillas
        v = MTrim(v, NoEval)

        sExpr = ""
        ' Buscar los caracteres a no evaluar y ponerlos despu�s de las asignaciones
        If Len(NoEval) Then
            Do
                j = InStr(v, NoEval)
                ' Si hay caracteres a no evaluar
                If j Then
                    ' Buscar el siguiente caracter NoEval y comprobar si despu�s
                    ' hay asignaciones
                    k = InStr(j + 1, v, NoEval, CompareMethod.Text)
                    If k = 0 Then k = Len(v)
                    sExpr = sExpr & Mid$(v, j, k - j + 1)
                    v = Left$(v, j - 1) & Mid$(v, k + 1)
                End If
            Loop While j
        End If

        ' Buscar el signo de dos puntos que ser� el separador,
        ' pero hay que tener en cuenta de que las asignaciones pueden ser con :=
        Do
            ' Buscar el siguiente signo igual
            i = InStr(v, "=")
            If i Then
                ' Lo que haya delante debe ser el nombre de la variable
                sNom = Left$(v, i - 1)
                ' Si sNom contiene : lo que haya antes de los dos puntos ser�
                ' parte de la expresi�n y el resto ser� el nombre.
                '*********************************************
                '***  Esto NO permite asignaciones con :=  ***
                '*********************************************
                j = InStr(sNom, ":")
                If j Then
                    sExpr = sExpr & Left$(sNom, j - 1)
                    sNom = Mid$(sNom, j + 1)
                End If
                ' Comprobar si a continuaci�n hay dos puntos,
                ' (ser� el separador de varias asignaciones)
                j = InStr(i + 1, v, ":", CompareMethod.Text)
                ' Si no hay, tomar la longitud completa restante
                If j = 0 Then j = Len(v) + 1
                ' Asignar el valor desde el signo igual hasta los dos puntos
                ' (o el fin de la cadena, valor de j)
                sVal = Mid$(v, i + 1, j - (i + 1))
                ' Dejar en v el resto de la cadena
                v = Mid$(v, j + 1)
                ' Si ya no hay nada m�s en la cadena, preparar para salir del Do
                If Len(v) = 0 Then i = 0
                ' Comprobar si est� en la lista de variables, si no est�, a�adirla
                j = IsVariable(sNom)
                If j Then
                    ' Esta variable ya existe, sustituir la expresi�n
                    '//////////////////////////////////////////////////////////////////
                    ' Si en la expresi�n asignada est� la misma variable
                    ' sustituirla por el valor que tuviera
                    sValAnt = aToken(sVal, sOp)
                    If sValAnt = sNom Then
                        ' Sustituir la variable por el valor
                        sVal = aVariables(j).Value & sOp & sVal
                        'aVariables(j).Value = sVal
                        ' y calcularlo
                        sVal = ParseFormula(sVal)
                        sVal = Calcular(sVal)
                    Else
                        ' En el caso que se reasigne el valor               (10/Feb/99)
                        sVal = sValAnt
                    End If
                    '//////////////////////////////////////////////////////////////////
                    ' Asignar el valor asignado
                    aVariables(j).Value = sVal
                Else
                    ' No existe la variable, a�adirla
                    NewVariable(sNom, sVal)
                End If
            End If
        Loop While i
        ' Devolver el resto de la cadena, si queda algo...
        AsignarVariables = sExpr & v
    End Function

    Private Function aToken(ByRef sF As String, ByRef sSimbol As String) As String
        '--------------------------------------------------------------------------
        ' Devuelve el siguiente TOKEN y el S�mbolo siguiente
        ' un Token es una variable, instrucci�n, funci�n o n�mero
        '
        ' Los par�metros se deben especificar por referencia ya que se modifican:
        '   sF          Cadena con la f�rmula o expresi�n a Tokenizar
        '   sSimbol     El s�mbolo u operador a usar
        ' Se devolver� la cadena con lo hallado o una cadena vac�a si no hay nada
        '--------------------------------------------------------------------------
        Dim i As Long
        Dim j As Long
        Dim sSimbs As String

        ' Usar los s�mbolos normales y los usados para los comentarios
        ' Pero no usar el de comillas dobles para que se puedan usar cadenas...
        'sSimbs = Chr$(34) & " " & Simbols & RemSimbs & " "
        sSimbs = Simbols & RemSimbs & " " & Chr(34) & " "
        'sSimbs = Simbols & RemSimbs & " "

        ' Si la cadena de entrada est� vac�a o s�lo tiene blancos
        If Len(Trim$(sF)) = 0 Then
            aToken = ""
            sF = ""
        Else
            j = MultInStr(sF, sSimbs, sSimbol)
            ' El valor devuelto ser� el s�mbolo que est� m�s a la izquierda
            If j = 0 Then
                ' Devolver la cadena completa
                aToken = sF
                sF = ""
            Else
                ' Si hay algo entre dos comillas dobles devolverlo          (10/Feb/99)
                i = InStr(sF, Chr(34))
                If i Then
                    ' Buscar la siguiente
                    j = InStr(i + 1, sF, Chr(34))
                    If j = 0 Then
                        sF = sF & Chr(34)
                        j = Len(sF)
                    End If
                    aToken = Mid$(sF, i, j - i + 1)
                    sF = Left$(sF, i - 1) & Mid$(sF, j + 1)
                    sSimbol = ""
                    Exit Function
                End If
                ' Devolver lo hallado hasta el token
                aToken = Left$(sF, j - 1)
                sF = Mid$(sF, j + Len(sSimbol))
                ' Si el n�mero est� en notaci�n cient�fica xxxEyyy
                If Right$(aToken, 1) = "E" Then
                    ' Comprobar si TODOS los caracteres anteriores a la E   (13/Feb/99)
                    ' son n�meros, ya que una variable o funci�n puede acabar con E
                    sSimbs = Left$(aToken, Len(aToken) - 1)
                    j = 0
                    For i = 1 To Len(sSimbs)
                        If InStr("0123456789", Mid$(sSimbs, i, 1)) Then
                            j = j + 1
                        End If
                    Next
                    ' Si el n�mero de cifras es igual a la longitud de la cadena
                    If j = Len(sSimbs) Then
                        ' Es que DEBER�A ser un n�mero con notaci�n cient�fica
                        '//////////////////////////////////////////////////////////////
                        ' IMPORTANTE:
                        '   No se procesar�n correctamente variables o funciones
                        '   que empiecen por n�meros y acaben con la letra E
                        '//////////////////////////////////////////////////////////////
                        aToken = aToken & sSimbol & Left$(sF, 2)
                        sF = Mid$(sF, 3)
                        sSimbol = ""
                    End If
                End If
            End If
        End If
    End Function

    Public Function MultInStr(ByVal String1 As String, ByVal String2 As String, _
                              Optional ByRef sSimb As String = "", Optional ByVal Start As Long = 1) As Long
        '--------------------------------------------------------------------------
        ' Siempre se especificar�n los tres par�metros,
        ' opcionalmente, el �ltimo ser� la posici�n de inicio o 1 si no se indica
        '
        ' Busca en la String1 cualquiera de los caracteres de la String2,
        ' devolviendo la posici�n del que est� m�s a la izquierda.
        ' El par�metro sSep se pasar� por referencia y en �l se devolver�
        ' el separador hallado.
        '
        ' En String2 se deber�n separar con espacios los caracteres a buscar
        '--------------------------------------------------------------------------
        Dim j As Long
        Dim sTmp As String
        ' La posici�n con un valor menor
        Dim elMenor As Long
        ' Caracter de separaci�n
        Const sSep As String = " "

        ' Hacer un bucle entre cada uno de los valores indicados en String2
        elMenor = 0
        If Start <= Len(String1) Then
            String2 = Trim$(String2) & sSep
            ' Se buscar�n todas las subcadenas de String2
            Do
                j = InStr(String2, sSep)
                If j Then
                    sTmp = Left$(String2, j - 1)
                    String2 = Mid$(String2, j + Len(sSep))
                    If Len(sTmp) Then
                        j = InStr(Start, String1, sTmp, CompareMethod.Text)
                    Else
                        j = 0
                    End If
                    If j Then
                        If elMenor = 0 Or elMenor > j Then
                            elMenor = j
                            sSimb = sTmp
                        End If
                        ' Si es la posici�n de inicio, no habr� ninguno menor
                        ' as� que salimos del bucle
                        If elMenor = Start Then
                            String2 = ""
                        End If
                    End If
                Else
                    String2 = ""
                End If
            Loop While Len(String2)
        End If
        MultInStr = elMenor
    End Function

    Public Sub New()
        '--------------------------------------------------------------------------
        ' Iniciar algunos valores y algunas de las funciones internas del VB
        ' soportadas por esta clase, (ver la constante FunVBNum)
        '--------------------------------------------------------------------------
        Dim i As Integer
        Dim sName As String
        Dim sFunVB As String
        '
        ' Por defecto, se devuelven los valores con notaci�n cient�fica (29/Ene/01)
        m_NotacionCientifica = True
        '
        ' S�mbolos
        Simbols = ":= < > = >= <= ( ) ^ * / \ - + $ ! # @ { } [ ] "
        ' Comentarios
        RemSimbs = "; ' // "
        ' Inicializar el array con el elemento cero, que no se usar�
        'ReDim aVariables(0)

        '--------------------------------------------------------------------------
        ' Quitada esta funci�n por defecto, ya que Sum() hace lo mismo  (14/Mar/99)
        '
        'ReDim aFunctions(0)
        '    ReDim aFunctions(1)
        '    ' Esta debe ser la primera funci�n para uso gener�rico,         (12/Feb/99)
        '    ' hasta 10 par�metros
        '    With aFunctions(1)
        '        ' Si hay m�s de 10 par�metros se sumar�n a lo que se haya puesto
        '        .Formula = "N1+N2+N3+N4+N5+N6+N7+N8+N9+N0"
        '        .Name = "<Gen�rica>"
        '        .Params = "N1,N2,N3,N4,N5,N6,N7,N8,N9,N0"
        '    End With
        '--------------------------------------------------------------------------
        ' Funciones con m�s de un par�metro,                            (09/Feb/99)
        ' incluso un n�mero indefinido.
        ' Los par�metros pueden ser cualquier tipo de expresi�n que esta clase evalue
        ' Cuando se usa m�s de un par�metro, asegurarse de que son nombres distintos
        ' por ejemplo "Num, Num" no funcionar�a, usar "Num, Num1"
        '--------------------------------------------------------------------------
        ' Suma varios n�meros, admite uno o m�s par�metros
        ' Suma dos n�meros, tambi�n admite s�lo un par�metro,
        ' Si se especifica uno, devuelve ese valor... luego no suma
        NewFunction("Sum", "Num1,Num2,...", "Num1+Num2+...")
        ' Resta n�meros
        NewFunction("Subs", "Num1,Num2,...", "Num1-Num2-...")
        ' Multiplica n�meros
        NewFunction("Mult", "Num1,Num2,...", "Num1*Num2*...")
        ' Las funciones que se van a evaluar de forma especial, se deben indicar con @
        ' aunque estas funciones deben estar previamente contempladas y s�lo se
        ' evaluan si est� el c�digo dentro de la clase...
        ' Max, devuelve el valor mayor de los dos indicados
        NewFunction("Max", "Num1,Num2", "@Max(Num1,Num2)")
        ' Min, devuelve el valor menor de los dos indicados
        NewFunction("Min", "Num1,Num2", "@Min(Num1,Num2)")
        '**************************************************************************
        '*** ATENCION ***
        '****************
        ' Si se usa Max o Min dentro de Max o Min hay que usar el s�mbolo @
        ' Por ejemplo: (devolver�a 20)
        ' Max(@Max(10,20),@Min(5,4))
        '**************************************************************************
        ' Nueva funci�n de redondeo                                     (11/Feb/99)
        'NewFunction "Round", "Num", "Int(Num+0.5)"
        '
        ' Funci�n Rnd del VB
        NewFunction("Rnd", "", "@Rnd")
        '
        ' Esta funci�n TIENE que tener el @                             (03/Nov/02)
        NewFunction("Bin", "Num,Precision", "@Bin(Num,Precision)")
        NewFunction("Dec2Bin", "Num", "Bin(Num)")
        NewFunction("Dec2Bin2", "Num,Precision", "Bin(Num,Precision)")
        '
        ' Logaritmo en base X
        NewFunction("LogX", "Num,Base", "Log(Num)/Log(Base)")
        '
        ' Para efectuar los c�culos en radianes y grados                (03/Nov/02)
        NewVariable("Pi", "3.1415926535897932384626433832795")
        NewFunction("Grados2Radianes", "Num", "Num*(Pi/180)")
        NewFunction("Radianes2Grados", "Num", "Num*(180/Pi)")
        'NewFunction "CosGrados", "Num", "(Cos(Num))*(180/Pi)"
        'NewFunction "CosGrados", "Num", "Cos(Num)*(180/Pi)"
        ' para probar, esto vuelve a ponerlo en radianes
        'NewFunction "Cos2", "Num", "CosGrados(Num)*Pi/180"
        'NewFunction "Cos2", "Num", "Grados2Radianes(CosGrados(Num))"
        '
        ' A�adir las declaradas en la constante FunVar
        ' Las funciones deben estar separadas por espacios y acabar con el (
        ' por ejemplo: "Int( Fix( "
        sFunVB = FunVBNum
        Do
            i = InStr(sFunVB, "( ")
            If i Then
                sName = Left$(sFunVB, i - 1)
                sFunVB = Mid$(sFunVB, i + 2)
                NewFunction(sName, "Num", "@" & sName & "(Num)")
            End If
        Loop While Len(sFunVB)
        ' A�adir otras para que sirvan de ejemplo
        ' No usar el signo @ si hacen uso de algunas de las definidas
        ' sino el resultado ser�a el de esa funci�n... esto habr� que arreglarlo...
        NewFunction("Sec", "Num", "1/Cos(Num)")
        NewFunction("CoSec", "Num", "1/Sin(Num)")
        NewFunction("CoTan", "Num", "1/Tan(Num)")
        '
        ' El n�mero de funciones propias de la clase,
        ' para evitar que se modifiquen
        mNumFunctions = UBound(aFunctions)
    End Sub

    Public Function IsVariable(ByVal sName As String) As Long
        '--------------------------------------------------------------------------
        ' Comprueba si es una variable,
        ' de ser as�, devolver� el �ndice en el array de variables
        ' o cero si no se ha hallado.
        ' En caso de no hallar la variable, la a�ade con el valor cero
        '--------------------------------------------------------------------------
        Dim i As Long

        sName = Trim$(sName)
        IsVariable = 0
        If Len(sName) Then
            For i = 1 To UBound(aVariables)
                If aVariables(i).Name = sName Then
                    IsVariable = i
                    Exit For
                End If
            Next
        End If
        ' Si no existe la variable
        ' No se hace nada, que es lo mismo que si no existiera...       (09/Feb/99)
        '    If IsVariable = 0 Then
        '        ' y no es un n�mero
        '        If Len(sName) Then
        '            If Not IsNumeric(sName) Then
        ''                ' Este caso ya no se dar�, pero por si las moscas       (08/Feb/99)
        ''                sVar = sName
        ''                If Right$(sName, 1) = "E" Then
        ''                    sName = sName & "-02"
        ''                End If
        ''                ' Por si es un n�mero con E
        ''                BuscarCifra sName, sVal
        ''                NewVariable sVar, sVal
        '                NewVariable sName, "0"
        '                IsVariable = UBound(aVariables)
        '            End If
        '        End If
        '    End If
    End Function

    Public Function IsFunction(ByVal sName As String) As Long
        '--------------------------------------------------------------------------
        ' Comprueba si es una funci�n,
        ' de ser as�, devolver� el �ndice en el array de f�rmulas
        ' o cero si no se ha hallado
        '--------------------------------------------------------------------------
        Dim i As Long
        '
        sName = Trim$(sName)
        IsFunction = 0
        If Len(sName) Then
            For i = 1 To UBound(aFunctions)
                If aFunctions(i).Name = sName Then
                    IsFunction = i
                    Exit For
                End If
            Next
        End If
    End Function

    Public Function FunctionVal(ByVal sName As String) As String
        '--------------------------------------------------------------------------
        ' Comprueba si sName contiene una f�rmula interna
        ' Estar� en el formato: @FormulaInterna
        ' S�lo ser� v�lido para funciones que no necesiten par�metros
        ' Ahora se permite un par�metro en las funciones soportadas     (08/Feb/99)
        '--------------------------------------------------------------------------
        Dim i As Long, j As Long
        Dim sValue As String, sParams As String, sNameFun As String
        '
        sName = Trim$(sName)
        i = InStr(sName, "@")
        esFunVB = False
        If i Then
            sValue = Left$(sName, i - 1) & Mid$(sName, i + 1)
            ' Si es Rnd
            i = InStr(sValue, "Rnd")
            ' El formato ser� Rnd [* valor]
            If i Then
                sName = Left$(sValue, i - 1) & "(" & Rnd() & ")" & Mid$(sValue, i + 3)
            End If
            '//////////////////////////////////////////////////////////////////////////
            ' Si es alguna de las definidas en la constante FunVBNum
            ' sNameFun devolver� el nombre de la funci�n hallada
            i = MultInStr(sValue, FunVBNum, sNameFun)
            ' El formato ser� NombreFunci�n(expresi�n)
            If i Then
                j = InStr(i, sValue, "(", CompareMethod.Text)
                ' esto s�lo permite funciones de tres letras
                'sName = Mid$(sValue, i + 3)
                If j = 0 Then j = 3
                sName = Mid$(sValue, j)
                sParams = parametros(sName)
                ' Calcular los par�metros
                sParams = Calcular(sParams)
                '
                esFunVB = True
                '
                ' Convertir el par�metro para usar con estas funciones num�ricas
                Select Case sNameFun
                    Case "Int("
                        sParams = Int(sParams)
                    Case "Fix("
                        sParams = Fix(sParams)
                    Case "Abs("
                        sParams = Abs(CDbl(sParams))
                    Case "Sgn("
                        sParams = Sign(CDbl(sParams))
                    Case "Sqr("
                        sParams = Sqrt(CDbl(sParams))
                    Case "Cos("
                        sParams = Cos(sParams)
                    Case "Sin("
                        sParams = Sin(sParams)
                    Case "Tan("
                        sParams = Tan(sParams)
                    Case "Atn("
                        sParams = Atan(sParams)
                    Case "Exp("
                        sParams = Exp(sParams)
                    Case "Log(", "Ln("  ' logaritmo natural (en base e)
                        sParams = Log(sParams)
                    Case "Log10("       ' logaritmo en base 10              (03/Nov/02)
                        sParams = Log(sParams) / Log(10)
                        ' Nuevas funciones                                      (02/Nov/02)
                    Case "Hex(", "Dec2Hex("
                        sParams = Hex(sParams)
                    Case "Oct(", "Dec2Oct("
                        sParams = Oct(sParams)
                    Case "Round("
                        sParams = Round(CDbl(sParams))
                    Case "Bin2Dec("
                        sParams = Bin2Dec(sParams)
                    Case "Hex2Dec("
                        sParams = Val("&H" & sParams)
                    Case "Oct2Dec("
                        sParams = Val("&O" & sParams)
                End Select
                sName = "(" & sParams & ")" & sName
            Else
                ' Algunas otras que se evaluar�n aqu�
                ' deben estar declaradas con @
                i = MultInStr(sValue, "Max( Min( Bin( ", sNameFun)
                ' El formato ser� Max(Num1, Num2)
                If i Then
                    On Error Resume Next
                    ' con esto s�lo se pueden tener funciones de 3 caracteres
                    'sName = Mid$(sValue, i + 3)
                    j = InStr(i, sValue, "(", CompareMethod.Text)
                    If j = 0 Then j = 3
                    sName = Mid$(sValue, j)
                    '
                    sParams = parametros(sName)
                    i = InStr(sParams, ",")
                    If i Then
                        sValue = Left$(sParams, i - 1)
                        sParams = Mid$(sParams, i + 1)
                        sValue = ParseFormula(sValue)
                        sParams = ParseFormula(sParams)
                        sValue = Calcular(sValue)
                        sParams = Calcular(sParams)
                        If sNameFun = "Max(" Then
                            sName = IIf(CDbl(sValue) > CDbl(sParams), sValue, sParams)
                        ElseIf sNameFun = "Min(" Then
                            sName = IIf(CDbl(sValue) < CDbl(sParams), sValue, sParams)
                        ElseIf sNameFun = "Bin(" Then
                            '
                            esFunVB = True
                            '
                            If Len(sParams) Then
                                sName = Dec2Bin(sValue, sParams)
                            Else
                                sName = Dec2Bin(sValue)
                            End If
                        End If
                        'Pendiente
                        'If Err() Then
                        '    sName = "Error: hay que usarla con @ o alguna variable no est� definida"
                        'End If
                        'On Error GoTo 0
                        'Err = 0
                    Else
                        sName = sParams
                    End If
                End If
            End If
            '//////////////////////////////////////////////////////////////////////////
        End If
        FunctionVal = sName
    End Function

    Public Function VariableVal(ByVal sName As String) As String
        '--------------------------------------------------------------------------
        ' Comprueba si es una variable,
        ' de ser as�, devolver� el contenido o valor de esa variable
        '
        ' Las variables estar�n en un array
        '--------------------------------------------------------------------------
        Dim i As Long
        Dim sValue As String
        '
        sName = Trim$(sName)
        sValue = ""
        If Len(sName) Then
            For i = 1 To UBound(aVariables)
                If aVariables(i).Name = sName Then
                    sValue = aVariables(i).Value
                    Exit For
                End If
            Next
        End If
        VariableVal = sValue
    End Function

    Public Sub NewFunction(ByVal sName As String, ByVal sParams As String, ByVal sFormula As String)
        '--------------------------------------------------------------------------
        ' Asigna una nueva funci�n al array de funciones
        ' Los par�metros ser�n el nombre, los par�metros y la f�rmula a usar
        '
        ' Si la funci�n indicada ya existe, se sustituir�n los valores especificados
        '--------------------------------------------------------------------------
        Dim i As Long
        Dim Hallado As Boolean
        Dim NumF As Long
        '
        sName = Trim$(sName)
        sParams = Trim$(sParams)
        sFormula = Trim$(sFormula)
        '
        NumF = UBound(aFunctions)
        Hallado = False
        ' Comprobar si la funci�n ya existe
        For i = LBound(aFunctions) To UBound(aFunctions)
            ' Si es as�, asignar el nuevo valor
            If aFunctions(i).Name = sName Then
                ' s�lo a�adirla si no es de las predefinidas            (03/Nov/02)
                If i > mNumFunctions Then
                    aFunctions(i).Params = sParams
                    aFunctions(i).Formula = sFormula
                End If
                Hallado = True
                Exit For
            End If
        Next
        ' Si no se ha hallado la funci�n, a�adirla
        If Not Hallado Then
            Dim a As New tFunctions
            With a
                .Name = sName
                .Params = sParams
                .Formula = sFormula
            End With
            NumF = NumF + 1
            ReDim Preserve aFunctions(NumF)
            aFunctions(NumF) = a
        End If
    End Sub

    Public Sub NewVariable(ByVal sName As String, ByVal sValue As String)
        '--------------------------------------------------------------------------
        ' Asigna una nueva variable al array de variables
        ' Los par�metros ser�n el nombre y el valor
        '
        ' Si la variable indicada ya existe, se sustituir� el valor por el indicado
        '--------------------------------------------------------------------------
        Dim i As Long
        Dim Hallado As Boolean
        Dim NumVars As Long
        '
        sName = Trim$(sName)
        sValue = Trim$(sValue)
        '
        NumVars = UBound(aVariables)
        Hallado = False
        ' Comprobar si la variable ya existe
        For i = LBound(aVariables) To UBound(aVariables)

            ' Si es as�, asignar el nuevo valor
            If aVariables(i).Name = sName Then
                aVariables(i).Value = sValue
                Hallado = True
                Exit For
            End If
        Next
        ' Si no se ha hallado la variable, a�adir una nueva
        If Not Hallado Then
            Dim a As New tVariable
            With a
                .Name = sName
                .Value = sValue
            End With
            NumVars = NumVars + 1
            ReDim Preserve aVariables(NumVars)
            aVariables(NumVars) = a
        End If
    End Sub

    Public Function ParseFormula(ByVal sF As String) As String
        '--------------------------------------------------------------------------
        ' Analiza la f�rmula indicada, sustituyendo las variables y funciones
        ' por sus valores, despu�s habr� que calcular el resultado devuelto.
        ' En esta funci�n se analizan las variables y funciones, dejando el valor
        ' que devolver�an.
        ' Si la variable o funci�n tiene otras variables o funciones se analizan
        ' y se ponen los valores devueltos.
        '--------------------------------------------------------------------------
        Dim qFuncion As Long
        Dim sFormula As String
        Dim sToken As String
        Dim sOp As String
        Dim sVar As String
        Dim sFunFormula As String
        '
        Do
            ' Asignar a sToken el siguiente elemento a procesar
            sOp = ""
            sToken = aToken(sF, sOp)
            ' Si no es una funci�n ni una variable, usar el valor indicado
            If Len(sToken) Then
                ' Si no es una variable o funci�n
                If Not IsFuncOrVar(sToken) Then
                    sFormula = sFormula & sToken & sOp
                Else
                    ' Comprobar si el Token es una variable,
                    ' si es as� sustituirla por el valor
                    sVar = VariableVal(sToken)
                    If Len(sVar) Then
                        ' Comprobar si la variable contiene otras variables
                        ' o funciones
                        sVar = ParseFormula(sVar)
                        ' Asigna a sToken el valor obtenido
                        sToken = Calcular(sVar)
                    End If
                    ' Comprobar si el Token es una funci�n
                    qFuncion = IsFunction(sToken)
                    ' Si es una funci�n, qFuncion tiene el �ndice de la funci�n
                    If qFuncion Then
                        ' Asignar los par�metros que usa la funci�n
                        sVar = aFunctions(qFuncion).Params
                        ' La f�rmula a usar para esta funci�n
                        sFunFormula = aFunctions(qFuncion).Formula
                        ' Si admite par�metros
                        If Len(sVar) Then
                            '//////////////////////////////////////////////////////////
                            ' Usar la funcion Parametros para analizar los pr�metros
                            '//////////////////////////////////////////////////////////
                            If sOp = "(" Then
                                sF = sOp & sF
                                'sParams = Parametros(sF)
                                'If Len(sParams) Then
                                sOp = ""
                                'End If
                            End If
                            sFunFormula = ConvertirParametros(sFunFormula, sVar, sF)
                        End If
                        ' Si tiene @FuncionInterna
                        ' usar esa funci�n
                        sVar = ""
                        sVar = FunctionVal(sFunFormula)
                        If Len(sVar) Then
                            sFunFormula = ParseFormula(sVar)
                        End If
                        'sFormula = sFormula & Calcular(sFunFormula & sOp & sF)
                        If sOp <> Chr(34) Then
                            sFunFormula = sFunFormula & sOp & sF
                            ' Esto daba problemillas                    (03/Nov/02)
                            ' a ver si as� se soluciona...
                            If sFormula = "(" Then
                                ' poner la del final
                                sFunFormula = sFormula & sFunFormula
                                sFormula = ""
                            End If
                            sFunFormula = ParseFormula(sFunFormula)
                            sFormula = sFormula & Calcular(sFunFormula)
                            sF = ""
                        Else
                            sFormula = sFormula & Calcular(sFunFormula)
                            sFormula = sFormula & sOp & sF
                            sF = ""
                        End If
                    Else
                        sFormula = sFormula & sToken & sOp
                    End If
                End If
            Else
                sFormula = sFormula & sToken & sOp
            End If
            '
        Loop While Len(sF)
        ' Devolver la expresi�n lista para calcular el valor
        ParseFormula = sFormula
    End Function

    Public Function Calcular(ByVal sFormula As String) As String
        '--------------------------------------------------------------------------
        ' Calcula el resultado de la expresi�n que entra en sFormula    (22/Oct/91)
        ' Modificado por la cuenta de la vieja...                 (01.12  7/May/93)
        ' Revisado para usar con cFormulas                              (06/Feb/99)
        '--------------------------------------------------------------------------
        Dim i As Long, j As Long, k As Long
        Dim j1 As Long, k1 As Long, n As Long
        Dim pn As Long
        Dim n1 As Object  'Double
        Dim n2 As Object  'Double
        Dim n3 As Object  'Double
        Dim Operador As String
        Dim Cifra1 As String
        Dim Cifra2 As String
        Dim strP As String
        Dim sOperadores As String
        ' Estos son los s�mbolos a buscar para el operador anterior
        ' se deben incluir los par�ntesis ya que estos separan precedencias
        Const cOperadores As String = "%^*/\+-()"
        '
        Static sFormulaAnt As String
        '
        ' Quitarle los espacios extras
        sFormula = Trim$(sFormula)
        '
        sOperadores = "% ^ * / \ "
        ' Si la f�rmula tiene una operaci�n, calcularla
        If MultInStr(sFormula, sOperadores) Then
            esFunVB = False
        End If
        '
        If esFunVB Then
            ' Si es una funci�n interna                                 (03/Nov/02)
            ' Devolver lo que ya se ha calculado, quitar los par�ntesis, etc.
            If Left$(sFormula, 1) = "@" Then sFormula = Trim$(Mid$(sFormula, 2))
            Do
                If Left$(sFormula, 1) = "(" Then
                    sFormula = Mid$(sFormula, 2)
                    If Right$(sFormula, 1) = ")" Then
                        sFormula = Left$(sFormula, Len(sFormula) - 1)
                    End If
                    sFormula = Trim$(sFormula)
                Else
                    Exit Do
                End If
            Loop
            Calcular = sFormula
            Exit Function
        End If
        '
        '//////////////////////////////////////////////////////////////////////////
        ' Para analizar siguiendo las operaciones de m�s "peso",        (07/Feb/99)
        ' se buscar�n operaciones en este orden % ^ * / \ + -
        ' y si se encuentran, se incluir�n entre par�ntesis para que se procesen
        ' antes que el resto:
        ' 25 + 100 * 3 se convertir�a en: 25 + (100 * 3)
        '
        ' Buscar cada uno de los operadores y a�adir los par�ntesis necesarios
        ' No se incluyen la suma y resta ya que son las que menos peso tienen
        sOperadores = "% ^ * / \ "
        ' S�lo procesar si tiene uno de los operadores
        If MultInStr(sFormula, sOperadores, Operador) Then
            Cifra1 = sFormula
            n = Len(Cifra1)
            For i = 1 To Len(sOperadores) Step 2
                Operador = Mid$(sOperadores, i, 1)
                ' Se deber�a buscar de atr�s para delante
                ' (ya se busca)
                pn = RInStr(n, Cifra1, Operador)
                If pn Then
                    ' Tenemos ese operador
                    ' buscar el signo anterior
                    k = 0
                    For j = pn - 1 To 1 Step -1
                        k = InStr(cOperadores, Mid$(Cifra1, j, 1))
                        If k Then
                            ' S�lo procesar si el signo anterior es diferente de )
                            If Mid$(cOperadores, k, 1) <> ")" Then
                                ' Buscar el signo siguiente
                                k1 = 0
                                For j1 = pn + 1 To Len(Cifra1)
                                    k1 = InStr(cOperadores, Mid$(Cifra1, j1, 1))
                                    If k1 Then
                                        ' A�adirle los par�ntesis
                                        ' Si se multiplica por un n�mero negativo
                                        k = MultInStr(Cifra1, "*- /- \- ")
                                        If k Then
                                            Cifra1 = Left$(Cifra1, j) & "(" & Mid$(Cifra1, j + 1, j1 - j - 2) & ")" & Mid$(Cifra1, k)
                                        Else
                                            If Right$(Mid$(Cifra1, j + 1, j1 - j - 1) & ")" & Mid$(Cifra1, j1, 1), 3) = "*)(" Then
                                                Cifra1 = Left$(Cifra1, j) & "(" & Mid$(Cifra1, j + 1, j1 - j - 2) & Mid$(Cifra1, j1 - 1) & ")"
                                            Else
                                                Cifra1 = Left$(Cifra1, j) & "(" & Mid$(Cifra1, j + 1, j1 - j - 1) & ")" & Mid$(Cifra1, j1)
                                            End If
                                        End If
                                        Exit For
                                    End If
                                Next
                                ' Si no hay ning�n signo siguiente
                                If k1 = 0 Then
                                    Cifra1 = Left$(Cifra1, j) & "(" & Mid$(Cifra1, j + 1) & ")"
                                End If
                            End If
                            Exit For
                        End If
                    Next
                    pn = RInStr(n, Cifra1, Operador)
                    n = pn - 1
                    i = i - 2
                End If
            Next
            sFormula = Cifra1
        End If
        '
        '//////////////////////////////////////////////////////////////////////////
        '
        ' Buscar par�ntesis e ir procesando las expresiones.
        Do While InStr(sFormula, "(")
            pn = InStr(sFormula, ")")
            ' Si hay par�ntesis de cierre
            If pn Then
                For i = pn To 1 Step -1
                    If Mid$(sFormula, i, 1) = "(" Then
                        ' Calcular lo que est� entre par�ntesis
                        strP = Mid$(sFormula, i + 1, pn - i - 1)
                        strP = Calcular(strP)
                        sFormula = Left$(sFormula, i - 1) & strP & Mid$(sFormula, pn + 1)
                        Exit For
                    End If
                Next
            Else
                sFormula = sFormula & ")"
            End If
        Loop

        ' Si la f�rmula a procesar tiene alg�n operador
        sOperadores = "% ^ * / \ + - "
        If MultInStr(sFormula, sOperadores, Operador) Then
            '//////////////////////////////////////////////////////////////////////
            ' Si hay m�s de un operador,                                (11/Ene/01)
            ' ponerlos dentro de par�ntesis seg�n el nivel de precedencia
            ' He a�adido el + y - ya que no hac�a los c�lculos bien     (22/Ene/01)
            '//////////////////////////////////////////////////////////////////////
            If MultipleStr2InStr1(sFormula, "%^*/\+-") Then
                '
                ' A ver si esto arregla los c�lculos "normales"         (22/Feb/01)
                ' ya que daba error al calcular: 3*2+5
                ' Gracias a Luis Americo Popiti
                '
                If Len(sFormulaAnt) = 0 Then
                    sFormulaAnt = sFormula
                End If
                If sFormulaAnt <> sFormula Then
                    sFormula = Calcular(sFormula)
                End If
                sFormulaAnt = ""
            End If
            Operador = ""
            Cifra1 = ""
            Cifra2 = ""
            Do
                ' Buscar la primera cifra
                If Len(sFormula) Then
                    If Cifra1 = "" Then
                        buscarCifra(sFormula, Cifra1)
                    End If
                    Operador = Left$(sFormula, 1)
                    sFormula = Mid$(sFormula, 2)
                    ' Buscar la segunda cifra
                    buscarCifra(sFormula, Cifra2)
                    '
                    n1 = 0
                    If Len(Cifra1) Then
                        n1 = CDec(Cifra1)
                    End If
                    ' Esto es necesario por si no se ponen los par�ntesis de apertura
                    n2 = 0
                    If Len(Cifra2) Then
                        n2 = CDec(Cifra2)
                    End If
                    ' Efectuar el c�lculo
                    Select Case Operador
                        Case "+"
                            n3 = n1 + n2
                        Case "-"
                            n3 = n1 - n2
                        Case "*"
                            n3 = n1 * n2
                            ' Si se divide por cero, se devuelve cero en lugar de dar error
                        Case "/"
                            If n2 <> 0.0# Then
                                n3 = n1 / n2
                            Else
                                n3 = 0.0#
                            End If
                        Case "\"
                            If n2 <> 0.0# Then
                                n3 = n1 \ n2
                            Else
                                n3 = 0.0#
                            End If
                        Case "^"
                            n3 = n1 ^ n2
                            ' C�lculo de porcentajes:
                            ' 100 % 25 = 25 (100 * (25 / 100))
                        Case "%"
                            n3 = n1 * CDec(n2 / CDec(100))
                            ' Si es comillas dobles, no evaluar
                        Case Chr(34)
                            ' Calcular el resto despu�s de las comillas
                            i = InStr(sFormula, Chr(34))
                            If i Then
                                Cifra1 = Mid$(sFormula, i + 1)
                                sFormula = Operador & Left$(sFormula, i)
                                Operador = ""
                                sFormula = sFormula & Calcular(Cifra1)
                                Calcular = sFormula
                                Exit Function
                            Else
                                sFormula = Operador & sFormula
                                Operador = ""
                                Calcular = sFormula
                                Exit Function
                            End If
                            ' Si no es una operaci�n reconocida, devolver la suma,
                            ' ya que esto puede ocurrir con los valores asignados a variables
                        Case Else
                            ' Por si se incluye una palabra que no est� declarada
                            ' (variable o funci�n)
                            If Len(Cifra1 & Cifra2) Then
                                If Len(Cifra1) = 0 Then
                                    Cifra1 = "0"
                                End If
                                If Len(Cifra2) = 0 Then
                                    Cifra2 = "0"
                                End If
                                n3 = CDec(Cifra1) + CDec(Cifra2)
                            Else
                                n3 = 0
                            End If
                    End Select
                    Cifra1 = CStr(n3)
                Else
                    Exit Do
                End If
            Loop While Operador <> ""
            Calcular = CStr(n3)
        Else
            ' Si no tiene ning�n operador, devolver la f�rmula
            ' Habr�a que quitarle los caracteres extra�os               (10/Feb/99)
            If Left$(sFormula, 1) <> Chr(34) Then
                ' tener en cuenta los n�meros hexadecimales             (03/Nov/02)
                sOperadores = "0123456789,.ABCDEF"
                Cifra1 = ""
                For i = 1 To Len(sFormula)
                    If InStr(sOperadores, Mid$(sFormula, i, 1)) Then
                        Cifra1 = Cifra1 & Mid$(sFormula, i, 1)
                    End If
                Next
                sFormula = Cifra1
            End If
            Calcular = sFormula
        End If
    End Function

    Public Function RInStr(ByVal v1 As Object, ByVal v2 As Object, _
                           Optional ByVal v3 As Object = Nothing) As Long
        '--------------------------------------------------------------------------
        ' Devuelve la posici�n de v2 en v1, empezando por atr�s
        '--------------------------------------------------------------------------
        Dim i As Long
        Dim posIni As Long
        Dim sTmp As String
        Dim s1 As String
        Dim s2 As String
        '
        If Length(v3) Then
            ' Si no se especifican los tres par�metros
            s1 = CStr(v1)       ' La primera cadena
            s2 = CStr(v2)       ' la segunda cadena
            posIni = Len(s1)    ' el �ltimo caracter de la cadena
        Else
            posIni = CLng(v1)   ' la posici�n por la que empezar
            s1 = CStr(v2)       ' la primera cadena (segundo par�metro)
            s2 = CStr(v3)       ' la segunda cadena (tercer par�metro)
        End If
        ' Valor inicial de la b�squeda, si no se encuentra, es cero
        RInStr = 0
        ' Siempre se empieza a buscar por el final
        For i = posIni - Len(s2) + 1 To 1 Step -1
            ' Tomar el n�mero de caracteres que tenga la segunda cadena
            sTmp = Mid$(s1, i, Len(s2))     ' Si son iguales...
            If sTmp = s2 Then               ' esa es la posici�n
                RInStr = i
                Exit For
            End If
        Next
    End Function

    Private Sub buscarCifra(ByRef Expresion As String, ByRef Cifra As String)
        '--------------------------------------------------------------------------
        ' Buscar en Expresion una cifra                             ( 5 / 10/May/93)
        ' Devuelve la cifra y el resto de la expresi�n
        '--------------------------------------------------------------------------
        Const OPERADORES As String = "+-*/\^%"
        Const CIFRAS As String = "0123456789., "
        Const POSITIVO As Long = 1&
        Const NEGATIVO As Long = -1&
        '
        Dim Signo As Long
        Dim ultima As Long
        Dim i As Long
        Dim s As String
        Dim sCifras As String
        Dim sSigno As String
        '
        ' Quitar los espacios del principio
        Expresion = LTrim$(Expresion)
        '
        ' Capturar errores por si se usan varios par�metros
        On Error Resume Next
        '
        ' Evaluar s�lo si no est� entre comillas
        If Left$(Expresion, 1) <> Chr(34) Then
            Signo = POSITIVO                    'Comprobar si es un n�mero negativo
            If Left$(Expresion, 1) = "-" Then
                Signo = NEGATIVO
                Expresion = Mid$(Expresion, 2)
            End If
            '
            ultima = 0
            s = ""
            For i = 1 To Len(Expresion)
                If InStr(CIFRAS, Mid$(Expresion, i, 1)) Then
                    s = s & Mid$(Expresion, i, 1)
                    ultima = i
                Else
                    Exit For
                End If
            Next i
            ' El val funciona s�lo si el decimal es el punto,
            ' cuando es una coma toma s�lo la parte entera
            If Len(s) Then
                ' Convertir adecuadamente los decimales
                s = ConvDecimal(s)
                Cifra = CStr((s) * Signo)
            Else
                Cifra = ""
            End If
            Expresion = LTrim$(Mid$(Expresion, ultima + 1))
            If Left$(Expresion, 1) = "E" Then
                ultima = Val(Mid$(Expresion, 3))
                sSigno = Mid$(Expresion, 2, 1)
                s = ""
                For i = 1 To ultima
                    s = s & "0"
                Next
                s = "1" & s
                If sSigno = "-" Then
                    'Cifra = CCur((Cifra) / (s))
                    Cifra = (Cifra) / (s)
                Else
                    'Cifra = CCur((Cifra) * (s))
                    Cifra = (Cifra) * (s)
                End If
                Expresion = Mid$(Expresion, 5)
            End If
        End If
        On Error GoTo 0
        'Err = 0
    End Sub

    Public Function ConvDecimal(ByVal strNum As String, _
                                Optional ByRef sDecimal As String = ",", _
                                Optional ByRef sDecimalNo As String = ".") As String
        '--------------------------------------------------------------------------
        ' Asigna el signo decimal adecuado (o lo intenta)                   (10/Ene/99)
        ' Devuelve una cadena con el signo decimal del sistema
        '--------------------------------------------------------------------------
        Dim i As Long, j As Long
        Dim sNumero As String
        '
        ' Averiguar el signo decimal
        sNumero = Format$(25.5, "#.#")
        If InStr(sNumero, ".") Then
            sDecimal = "."
            sDecimalNo = ","
        Else
            sDecimal = ","
            sDecimalNo = "."
        End If
        '
        strNum = Trim$(strNum)
        If Left$(strNum, 1) = sDecimalNo Then
            Mid$(strNum, 1, 1) = sDecimal
        End If
        '
        ' Si el n�mero introducido contiene signos no decimales
        j = 0
        i = 1
        Do
            i = InStr(i, strNum, sDecimalNo, CompareMethod.Text)
            If i Then
                j = j + 1
                i = i + 1
            End If
        Loop While i
        '
        If j = 1 Then
            ' Cambiar ese s�mbolo por un espacio, si s�lo hay uno de esos signos
            i = InStr(strNum, sDecimalNo)
            If i Then
                If InStr(strNum, sDecimal) Then
                    Mid$(strNum, i, 1) = " "
                Else
                    Mid$(strNum, i, 1) = sDecimal
                End If
            End If
        Else
            ' En caso de que tenga m�s de uno de estos s�mbolos
            ' convertirlos de manera adecuada.
            ' Por ejemplo:
            ' si el signo decimal es la coma:
            '   1,250.45 ser�a 1.250,45 y quedar�a en 1250,45
            ' si el signo decimal es el punto:
            '   1.250,45 ser�a 1,250.45 y quedar�a en 1250.45
            '
            ' Aunque no se arreglar� un n�mero err�neo:
            ' si el signo decimal es la coma:
            '   1,250,45 ser� lo mismo que 1,25
            '   12,500.25 ser� lo mismo que 12,50
            ' si el signo decimal es el punto:
            '   1.250.45 ser� lo mismo que 1.25
            '   12.500,25 ser� lo mismo que 12.50
            '
            i = 1
            Do
                i = InStr(i, strNum, sDecimalNo, CompareMethod.Text)
                If i Then
                    j = j - 1
                    If j = 0 Then
                        Mid$(strNum, i, 1) = sDecimal
                    Else
                        Mid$(strNum, i, 1) = " "
                    End If
                    i = i + 1
                End If
            Loop While i
        End If
        '
        j = 0
        ' Quitar los espacios que haya por medio
        Do
            i = InStr(strNum, " ")
            If i = 0 Then Exit Do
            strNum = Left$(strNum, i - 1) & Mid$(strNum, i + 1)
        Loop
        '
        ConvDecimal = strNum
    End Function



    Public Sub ShowFunctions(ByRef aList As Object)
        '--------------------------------------------------------------------------
        ' Devuelve las funciones y las f�rmulas usadas en el formato:
        '   Nombre = Funci�n | Par�mentros
        ' El par�metro indicar� una colecci�n o un ListBox/ComboBox
        '--------------------------------------------------------------------------
        Dim i As Long
        '
        For i = 1 To UBound(aFunctions)
            With aFunctions(i)
                If TypeOf aList Is Collection Then
                    aList.Add(.Name & " = " & .Formula & " | " & .Params)
                Else
                    aList.AddItem(.Name & " = " & .Formula & " | " & .Params)
                End If
            End With
        Next
    End Sub

    Public Sub ShowVariables(ByVal aList As Object)
        '--------------------------------------------------------------------------
        ' Devuelve las variables y los valores en el formato:
        '   Nombre = Valor
        ' El par�metro indicar� una colecci�n o un ListBox/ComboBox
        '--------------------------------------------------------------------------
        Dim i As Long
        '
        For i = 1 To UBound(aVariables)
            If TypeOf aList Is Collection Then
                aList.Add(aVariables(i).Name & " = " & aVariables(i).Value)
            Else
                aList.AddItem(aVariables(i).Name & " = " & aVariables(i).Value)
            End If
        Next
    End Sub

    Public Function Formula(ByVal sExpresion As String) As String
        '--------------------------------------------------------------------------
        ' Esta funci�n calcula directamente la expresi�n
        '--------------------------------------------------------------------------
        Dim tmpCientifica As Boolean
        Dim s As String
        '
        tmpCientifica = m_NotacionCientifica
        '
        ' Comprobar si hay asignaciones en la expresi�n
        sExpresion = AsignarVariables(sExpresion, Chr(34))
        ' Si se usa Bin, Bin2Dec o Dec2Bin no usar notaci�n cientifica  (02/Nov/02)
        If MultInStr(sExpresion, "Bin( Bin2Dec Dec2Bin Hex( Oct( ") Then
            m_NotacionCientifica = False
        End If
        ' Interpretar la expresi�n
        sExpresion = ParseFormula(sExpresion)
        '
        '
        ' Calcular la expresi�n
        s = Calcular(sExpresion)
        '
        ' Convertir el resultado en Double                              (29/Ene/01)
        ' Si as� se ha especificado en la propiedad NotacionCientifica,
        ' que por defecto es True
        '
        ' Si da error, usar el valor devuelto por Calcular
        On Error Resume Next
        '
        If m_NotacionCientifica Then
            Formula = CDbl(s)
            'Pendiente
            'If Err() Then
            '    Formula = s
            'End If
        Else
            Formula = s
        End If
        '
        m_NotacionCientifica = tmpCientifica
        'Pendiente
        '  Err = 0
    End Function

    Public Function IsFuncOrVar(ByVal sName As String) As Boolean
        '--------------------------------------------------------------------------
        ' Comprobar si es una funci�n o variable

        ' Es importante comprobar primero las funciones
        ' para que no se a�ada una funci�n como si fuese una variable no declarada
        '--------------------------------------------------------------------------
        ' Si no es un n�mero
        If Not IsNumeric(sName) Then
            If IsFunction(sName) Then
                IsFuncOrVar = True
            ElseIf IsVariable(sName) Then
                IsFuncOrVar = True
            End If
        End If
    End Function

    Private Function parametros(ByRef sExp As String) As String
        '--------------------------------------------------------------------------
        ' Devolver� los par�metros de la expresi�n pasada por referencia(08/Feb/99)
        ' Los par�metros deben estar encerrados entre par�ntesis
        ' En sExp, se devolver� el resto de la cadena.
        '--------------------------------------------------------------------------
        Dim i As Long, j As Long, k As Long
        Dim sParams As String
        Dim sExpAnt As String
        '
        sExp = Trim$(sExp)
        sExpAnt = sExp
        '
        '
        ' Buscarlos, estar�n entre par�ntesis
        '
        If Left$(sExp, 1) = "(" Then
            sExp = Mid$(sExp, 2)
            ' Buscar el siguiente )
            k = 0
            j = 0
            For i = 1 To Len(sExp)
                If Mid$(sExp, i, 1) = "(" Then
                    j = j + 1
                End If
                If Mid$(sExp, i, 1) = ")" Then
                    j = j - 1
                    If j = -1 Then
                        k = i
                        Exit For
                    End If
                End If
            Next
            If k Then
                sParams = Left$(sExp, k - 1)
                sExp = Mid$(sExp, k + 1)
            End If
        Else
            sParams = ""
            sExp = sExpAnt
        End If
        '
        parametros = sParams
    End Function

    Public Function FunctionParams(ByVal sName As String) As String
        ' Devuelve los par�metros de la funci�n indicada                (12/Feb/99)
        Dim i As Long
        '
        sName = Trim$(sName)
        FunctionParams = ""
        If Len(sName) Then
            For i = 1 To UBound(aFunctions)
                If aFunctions(i).Name = sName Then
                    FunctionParams = aFunctions(i).Params
                    Exit For
                End If
            Next
        End If
    End Function

    Public Function FunctionFormula(ByVal sName As String) As String
        ' Devuelve la f�rmula de la funci�n indicada                    (10/Feb/99)
        Dim i As Long
        '
        sName = Trim$(sName)
        FunctionFormula = ""
        If Len(sName) Then
            For i = 1 To UBound(aFunctions)
                If aFunctions(i).Name = sName Then
                    FunctionFormula = aFunctions(i).Formula
                    Exit For
                End If
            Next
        End If
    End Function

    Public Function ConvertirParametros(ByVal sFunFormula As String, _
                                        ByVal sVar As String, _
                                        ByRef sF As String) As String
        '--------------------------------------------------------------------------
        ' Sustituir par�metros                                          (12/Feb/99)
        ' Sustituye en sFunFormula los par�metros indicados en sVar que est�n
        ' en la expresi�n sF.
        ' Devuelve el valor procesado.
        ' sF debe pasarse por referencia, ya que se devovler� lo que quede despu�s
        ' de procesarse los par�metros
        '--------------------------------------------------------------------------
        Dim i As Long, j As Long, k As Long, n As Long
        Dim sParams As String
        Dim sParamF As String       ' Par�metro en la f�rmula
        Dim sParamE As String       ' Par�metro en la expresi�n
        Dim sParamX As String       ' Para a�adir par�metros a la f�rmula
        '
        ConvertirParametros = ""
        '//////////////////////////////////////////////////////////
        ' Usar la funcion Parametros para analizar los pr�metros
        '//////////////////////////////////////////////////////////
        sParams = parametros(sF)
        '//////////////////////////////////////////////////////////
        If Len(sParams) Then
            ' Comprobar si los par�metros contienen alguna variable
            ' u otra funci�n
            sParams = ParseFormula(sParams)
            '
            ' Sustituir los par�metros por los indicados en la f�rmula
            ' (en principio s�lo se admite uno)
            ' Sustituir en la f�rmula el nombre de la variable
            ' por el par�metro
            '
            ' Si s�lo tiene un par�metro
            If InStr(sVar, ",") = 0 Then
                ' comprobar si sParams tiene m�s de uno
                i = InStr(sParams, ",")
                If i Then
                    ' De ser as�, quedarse s�lo con el primero
                    sParams = Trim$(Left$(sParams, i - 1))
                    ' Puede que los par�metros estuviesen ente par�ntesis
                    If Left$(sParams, 1) = "(" Then
                        ' Si le falta el del final, a�adirselo
                        If Right$(sParams, 1) <> ")" Then
                            sParams = sParams & ")"
                        End If
                    End If
                End If
                Do
                    ' Si sVar es una cadena vac�a,                      (29/Oct/02)
                    ' esta comprobaci�n dar� un nuevo positivo
                    i = InStr(sFunFormula, sVar)
                    If Len(sVar) = 0 Then i = 0
                    If i Then
                        ' Poner los par�metros dentro de par�ntesis
                        sFunFormula = Left$(sFunFormula, i - 1) & "(" & sParams & ")" & Mid$(sFunFormula, i + Len(sVar))
                    End If
                    ' Por si se queda colgado convirtiendo par�metros...
                Loop While i > 0 And Len(sFunFormula) < 3072&
            Else
                ' Resolver los par�metros
                ' sParams tiene los par�metros a evaluar
                ' sVar tiene los nombres de los par�metros
                sVar = sVar & ","
                sParams = sParams & ","
                If InStr(sFunFormula, "...") Then
                    ' Contar el n�mero de par�metros que se han pasado
                    ' para el caso de par�metros opcionales (se usan ...)
                    i = 0
                    For j = 1 To Len(sParams)
                        If Mid$(sParams, j, 1) = "," Then i = i + 1
                    Next
                    ' Para convertir los par�metros opcionales
                    ' en variables que despu�s se puedan sustituir.
                    ' Las variables deben ser diferentes.
                    sParamX = "NumX"
                    n = 0
                    ' Obtener el �ltimo par�metro de la f�rmula
                    sParamF = Right$(sFunFormula, 4)
                    sFunFormula = Left$(sFunFormula, Len(sFunFormula) - 4)
                    sVar = Left$(sVar, Len(sVar) - 5)
                    Do
                        k = 0
                        For j = 1 To Len(sVar)
                            If Mid$(sVar, j, 1) = "," Then k = k + 1
                        Next
                        If i > k + 1 Then
                            ' Buscar el �ltimo de sVar
                            For j = Len(sVar) - 1 To 1 Step -1
                                If Mid$(sVar, j, 1) = "," Then
                                    ' De esta forma aceptar� hasta 100 par�metros   (14/Feb/99)
                                    sVar = sVar & "," & sParamX & Format$(n, "00") ' CStr(n)
                                    sFunFormula = sFunFormula & Left$(sParamF, 1) & sParamX & Format$(n, "00") 'CStr(n)
                                    n = n + 1
                                    Exit For
                                End If
                            Next
                        End If
                    Loop While i > k + 1
                    If Right$(sVar, 1) <> "," Then sVar = sVar & ","
                End If
                '
                Do
                    j = InStr(sVar, ",")
                    If j Then
                        sParamF = Trim$(Left$(sVar, j - 1))
                        sVar = Trim$(Mid$(sVar, j + 1))
                        i = InStr(sParams, ",")
                        If i Then
                            sParamE = Trim$(Left$(sParams, i - 1))
                            sParams = Trim$(Mid$(sParams, i + 1))
                        Else
                            sParamE = sParams
                            sParams = ""
                        End If
                        ' Reemplazar sParamF por el par�metro
                        Do
                            ' Si sParamF es una cadena vac�a,           (29/Oct/02)
                            ' esta comprobaci�n dar� un nuevo positivo
                            i = InStr(sFunFormula, sParamF)
                            If Len(sParamF) = 0 Then i = 0
                            If i Then
                                ' Poner los par�metros dentro de par�ntesis
                                sFunFormula = Left$(sFunFormula, i - 1) & "(" & sParamE & ")" & Mid$(sFunFormula, i + Len(sParamF))
                            End If
                        Loop While i
                    End If
                Loop While j
            End If
            ConvertirParametros = sFunFormula
        End If
    End Function

    Public Function MultipleStr2InStr1(ByVal Str1 As String, _
                                       ByVal Str2 As String) As Boolean
        '--------------------------------------------------------------------------
        ' Devuelve True si:                                             (11/Ene/01)
        '   Str1 tiene m�s de un caracter de los indicados en Str2
        '--------------------------------------------------------------------------
        Dim i As Long
        Dim n As Long
        '
        ' Buscar cada uno de los caracteres de Str2 en Str1
        n = 0
        For i = 1 To Len(Str2)
            ' Comprobar si est�
            If InStr(Str1, Mid$(Str2, i, 1)) Then
                ' si es as�, incrementar el contador
                n = n + 1
                ' si ya se han encontrado m�s de uno, no seguir buscando
                If n > 1 Then Exit For
            End If
        Next
        MultipleStr2InStr1 = (n > 1)
    End Function
    Property NotacionCientifica() As Boolean
        Get
            Return m_NotacionCientifica
        End Get
        Set(ByVal Value As Boolean)
            m_NotacionCientifica = Value
        End Set
    End Property


    Public Function IsFunVB(ByVal sName As String) As Boolean
        ' Comprueba si la funci�n indicada es una funci�n de VB         (02/Nov/02)
        ' (las usadas en FunVBNum)
        Dim i As Long
        Dim sValue As String
        '
        i = InStr(sName, "@")
        If i Then
            sValue = Left$(sName, i - 1) & Mid$(sName, i + 1)
        Else
            sValue = sName
        End If
        '//////////////////////////////////////////////////////////////////////////
        ' Si es alguna de las definidas en la constante FunVBNum
        i = MultInStr(sValue, FunVBNum)
        '
        IsFunVB = CBool(i)
    End Function

    Public Function Dec2Bin(ByVal n As Long, _
                             Optional ByVal nCifras As Long = 16) As String
        ' Convertir el n�mero indicado a binario
        Dim i As Long
        Dim s As String
        '
        On Error GoTo Err2Bin
        s = ""
        For i = nCifras - 1 To 0 Step -1
            If n And (2 ^ i) Then
                s = s & "1"
            Else
                s = s & "0"
            End If
        Next
        Dec2Bin = s
        Exit Function
Err2Bin:
        Dec2Bin = "Error: " & Err.Description & ", al convertir 2^" & CStr(i)
    End Function

    Public Function Bin2Dec(ByVal sDec As String) As Long
        ' Convierte un n�mero binario en decimal
        ' El par�metro deber�a ser un n�mero con s�lo 1 y ceros,
        ' pero se considerar� como cero, cualquier car�cter que no sea un uno,
        ' excepto los espacios que no se tendr�n en cuenta.
        Dim n As Long
        Dim i As Long, j As Long
        Dim C As String
        '
        i = 0
        For j = Len(sDec) To 1 Step -1
            C = Mid$(sDec, j, 1)
            If C = "1" Then
                n = n + 2 ^ i
                i = i + 1
            ElseIf C <> " " Then
                i = i + 1
            End If
        Next
        Bin2Dec = n
    End Function


End Class
