namespace ITS291;

using Spectre.Console.Rendering;

public static class Extensions {
    // Extension method that adds an IEnumerable of selection groups to a SelectionPrompt
    public static SelectionPrompt<T> AddGroups<T>(this SelectionPrompt<T> prompt, IEnumerable<(T, IEnumerable<T>)> groups) {
        foreach (var (groupName, group) in groups) prompt.AddChoiceGroup(groupName, group);
        return prompt;
    }

    public static T[] ArrConcat<T>(this T[] arr, T[] otherArr) {
        return arr.Concat(otherArr).ToArray();
    }
    
    public static Table AddRows<T>(this Table table, IEnumerable<T> rows, Func<T, IEnumerable<IRenderable>> rowFunc) {
        foreach (var row in rows) table.AddRow(rowFunc(row));
        return table;
    }

    public static void Write(this IAnsiConsole console, params FormattableString[] sections) {
        foreach (var section in sections) {
            console.Write(Markup.FromInterpolated(section));
        }
    }
    public static void WriteLine(this IAnsiConsole console, params FormattableString[] sections) {
        Write(console, sections);
        AnsiConsole.WriteLine();
    }
    public static void Write(this IAnsiConsole console, params IRenderable[] sections) {
        foreach (var section in sections) {
            console.Write(section);
        }
    }
    public static void WriteLine(this IAnsiConsole console, params IRenderable[] sections) {
        Write(console, sections);
        AnsiConsole.WriteLine();
    }

    public static void Write(this IAnsiConsole console, FormattableString arg1, IRenderable arg2) {
        Write(console, Markup.FromInterpolated(arg1), arg2);
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, IRenderable arg2) {
        WriteLine(console, Markup.FromInterpolated(arg1), arg2);
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, FormattableString arg2) {
        Write(console, arg1, Markup.FromInterpolated(arg2));
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, FormattableString arg2) {
        WriteLine(console, arg1, Markup.FromInterpolated(arg2));
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, FormattableString arg2, IRenderable arg3) {
        Write(console, Markup.FromInterpolated(arg1), Markup.FromInterpolated(arg2), arg3);
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, FormattableString arg2, IRenderable arg3) {
        WriteLine(console, Markup.FromInterpolated(arg1), Markup.FromInterpolated(arg2), arg3);
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, FormattableString arg3) {
        Write(console, Markup.FromInterpolated(arg1), arg2, Markup.FromInterpolated(arg3));
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, FormattableString arg3) {
        WriteLine(console, Markup.FromInterpolated(arg1), arg2, Markup.FromInterpolated(arg3));
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, IRenderable arg3) {
        Write(console, Markup.FromInterpolated(arg1), arg2, arg3);
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, IRenderable arg3) {
        WriteLine(console, Markup.FromInterpolated(arg1), arg2, arg3);
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, FormattableString arg3) {
        Write(console, arg1, Markup.FromInterpolated(arg2), Markup.FromInterpolated(arg3));
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, FormattableString arg3) {
        WriteLine(console, arg1, Markup.FromInterpolated(arg2), Markup.FromInterpolated(arg3));
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, IRenderable arg3) {
        Write(console, arg1, Markup.FromInterpolated(arg2), arg3);
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, IRenderable arg3) {
        WriteLine(console, arg1, Markup.FromInterpolated(arg2), arg3);
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, IRenderable arg2, FormattableString arg3) {
        Write(console, arg1, arg2, Markup.FromInterpolated(arg3));
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, IRenderable arg2, FormattableString arg3) {
        WriteLine(console, arg1, arg2, Markup.FromInterpolated(arg3));
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, FormattableString arg2, FormattableString arg3, IRenderable arg4) {
        Write(console, Markup.FromInterpolated(arg1), Markup.FromInterpolated(arg2), Markup.FromInterpolated(arg3), arg4);
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, FormattableString arg2, FormattableString arg3, IRenderable arg4) {
        WriteLine(console, Markup.FromInterpolated(arg1), Markup.FromInterpolated(arg2), Markup.FromInterpolated(arg3), arg4);
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, FormattableString arg2, IRenderable arg3, FormattableString arg4) {
        Write(console, Markup.FromInterpolated(arg1), Markup.FromInterpolated(arg2), arg3, Markup.FromInterpolated(arg4));
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, FormattableString arg2, IRenderable arg3, FormattableString arg4) {
        WriteLine(console, Markup.FromInterpolated(arg1), Markup.FromInterpolated(arg2), arg3, Markup.FromInterpolated(arg4));
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, FormattableString arg2, IRenderable arg3, IRenderable arg4) {
        Write(console, Markup.FromInterpolated(arg1), Markup.FromInterpolated(arg2), arg3, arg4);
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, FormattableString arg2, IRenderable arg3, IRenderable arg4) {
        WriteLine(console, Markup.FromInterpolated(arg1), Markup.FromInterpolated(arg2), arg3, arg4);
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, FormattableString arg3, FormattableString arg4) {
        Write(console, Markup.FromInterpolated(arg1), arg2, Markup.FromInterpolated(arg3), Markup.FromInterpolated(arg4));
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, FormattableString arg3, FormattableString arg4) {
        WriteLine(console, Markup.FromInterpolated(arg1), arg2, Markup.FromInterpolated(arg3), Markup.FromInterpolated(arg4));
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, FormattableString arg3, IRenderable arg4) {
        Write(console, Markup.FromInterpolated(arg1), arg2, Markup.FromInterpolated(arg3), arg4);
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, FormattableString arg3, IRenderable arg4) {
        WriteLine(console, Markup.FromInterpolated(arg1), arg2, Markup.FromInterpolated(arg3), arg4);
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, IRenderable arg3, FormattableString arg4) {
        Write(console, Markup.FromInterpolated(arg1), arg2, arg3, Markup.FromInterpolated(arg4));
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, IRenderable arg3, FormattableString arg4) {
        WriteLine(console, Markup.FromInterpolated(arg1), arg2, arg3, Markup.FromInterpolated(arg4));
    }
    public static void Write(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, IRenderable arg3, IRenderable arg4) {
        Write(console, Markup.FromInterpolated(arg1), arg2, arg3, arg4);
    }
    public static void WriteLine(this IAnsiConsole console, FormattableString arg1, IRenderable arg2, IRenderable arg3, IRenderable arg4) {
        WriteLine(console, Markup.FromInterpolated(arg1), arg2, arg3, arg4);
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, FormattableString arg3, FormattableString arg4) {
        Write(console, arg1, Markup.FromInterpolated(arg2), Markup.FromInterpolated(arg3), Markup.FromInterpolated(arg4));
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, FormattableString arg3, FormattableString arg4) {
        WriteLine(console, arg1, Markup.FromInterpolated(arg2), Markup.FromInterpolated(arg3), Markup.FromInterpolated(arg4));
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, FormattableString arg3, IRenderable arg4) {
        Write(console, arg1, Markup.FromInterpolated(arg2), Markup.FromInterpolated(arg3), arg4);
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, FormattableString arg3, IRenderable arg4) {
        WriteLine(console, arg1, Markup.FromInterpolated(arg2), Markup.FromInterpolated(arg3), arg4);
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, IRenderable arg3, FormattableString arg4) {
        Write(console, arg1, Markup.FromInterpolated(arg2), arg3, Markup.FromInterpolated(arg4));
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, IRenderable arg3, FormattableString arg4) {
        WriteLine(console, arg1, Markup.FromInterpolated(arg2), arg3, Markup.FromInterpolated(arg4));
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, IRenderable arg3, IRenderable arg4) {
        Write(console, arg1, Markup.FromInterpolated(arg2), arg3, arg4);
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, FormattableString arg2, IRenderable arg3, IRenderable arg4) {
        WriteLine(console, arg1, Markup.FromInterpolated(arg2), arg3, arg4);
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, IRenderable arg2, FormattableString arg3, FormattableString arg4) {
        Write(console, arg1, arg2, Markup.FromInterpolated(arg3), Markup.FromInterpolated(arg4));
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, IRenderable arg2, FormattableString arg3, FormattableString arg4) {
        WriteLine(console, arg1, arg2, Markup.FromInterpolated(arg3), Markup.FromInterpolated(arg4));
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, IRenderable arg2, FormattableString arg3, IRenderable arg4) {
        Write(console, arg1, arg2, Markup.FromInterpolated(arg3), arg4);
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, IRenderable arg2, FormattableString arg3, IRenderable arg4) {
        WriteLine(console, arg1, arg2, Markup.FromInterpolated(arg3), arg4);
    }
    public static void Write(this IAnsiConsole console, IRenderable arg1, IRenderable arg2, IRenderable arg3, FormattableString arg4) {
        Write(console, arg1, arg2, arg3, Markup.FromInterpolated(arg4));
    }
    public static void WriteLine(this IAnsiConsole console, IRenderable arg1, IRenderable arg2, IRenderable arg3, FormattableString arg4) {
        WriteLine(console, arg1, arg2, arg3, Markup.FromInterpolated(arg4));
    }
}

