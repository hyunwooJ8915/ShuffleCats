using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

/// <summary>
/// [자동화 파서 시스템]
/// 1. Reflection을 활용하여 CSV 텍스트를 C# 객체로 자동 변환합니다.
/// 2. 지원 타입: 기본 타입(int, float, string), Enum, 배열(; 구분), 딕셔너리(Key:Value).
/// 3. RFC 4180 호환: 큰따옴표(") 안의 콤마/개행은 셀 내용으로 보존되고,
///    이스케이프된 큰따옴표("")는 한 개의 "로 취급됩니다.
/// </summary>
public static class CSVSerializer
{
    #region Variables
    private const char ArraySeparator = ';';
    private const char MapSeparator = ':';
    #endregion

    #region PublicMethod
    public static List<T> ParseCSV<T>(string csvText) where T : new()
    {
        List<T> list = new List<T>();
        List<List<string>> rows = ParseCsvText(csvText);

        if (rows.Count < 1) return list;

        // 1. 헤더 분석 (첫 줄에서 변수 이름 추출)
        List<string> headers = rows[0];

        // 2. 데이터 줄부터 반복
        // (첫 줄은 변수명, 두 번째 줄은 데이터 설명)
        for (int i = 2; i < rows.Count; i++)
        {
            List<string> columns = rows[i];
            T obj = new T();
            for (int j = 0; j < headers.Count; j++)
            {
                FieldInfo field = typeof(T).GetField(headers[j].Trim());
                if (field != null && j < columns.Count)
                {
                    SetFieldValue(obj, field, columns[j].Trim());
                }
            }
            list.Add(obj);
        }
        return list;
    }
    #endregion

    #region PrivateMethod
    /// <summary>
    /// CSV 텍스트를 행/셀 단위로 파싱합니다.
    /// 큰따옴표(")로 감싼 셀 안의 콤마와 개행은 구분자로 취급되지 않습니다.
    /// </summary>
    private static List<List<string>> ParseCsvText(string csv)
    {
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // "" → 셀 안의 리터럴 따옴표 한 개
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (c == '\r' || c == '\n')
                {
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();
                    CommitRow(rows, currentRow);
                    currentRow = new List<string>();

                    // \r\n 한 쌍을 하나의 줄바꿈으로 취급
                    if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        // 끝줄 처리 (개행 없이 EOF로 끝나는 경우)
        if (currentField.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentField.ToString());
            CommitRow(rows, currentRow);
        }

        return rows;
    }

    private static void CommitRow(List<List<string>> rows, List<string> row)
    {
        // 모든 셀이 비어있는 줄은 건너뜀 (기존 RemoveEmptyEntries 거동과 일치)
        for (int k = 0; k < row.Count; k++)
        {
            if (!string.IsNullOrEmpty(row[k]))
            {
                rows.Add(row);
                return;
            }
        }
    }

    private static void SetFieldValue(object obj, FieldInfo field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        Type type = field.FieldType;

        if (type.IsArray)
        { // 배열 처리
            string[] elements = value.Split(ArraySeparator);
            Type elemType = type.GetElementType();
            Array newArray = Array.CreateInstance(elemType, elements.Length);
            for (int i = 0; i < elements.Length; i++)
                newArray.SetValue(Convert.ChangeType(elements[i].Trim(), elemType), i);
            field.SetValue(obj, newArray);
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        { // List<T> 처리 (기본 타입 및 Enum, ; 구분)
            Type elemType = type.GetGenericArguments()[0];
            string[] elements = value.Split(ArraySeparator);
            IList list = (IList)Activator.CreateInstance(type);

            foreach (var e in elements)
            {
                string trimmed = e.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                list.Add(elemType.IsEnum
                    ? Enum.Parse(elemType, trimmed)
                    : Convert.ChangeType(trimmed, elemType));
            }
            field.SetValue(obj, list);
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        { // 딕셔너리 처리
            string[] pairs = value.Split(ArraySeparator);
            Type[] argTypes = type.GetGenericArguments();
            IDictionary dict = (IDictionary)Activator.CreateInstance(type);
            foreach (var p in pairs)
            {
                string[] kv = p.Split(MapSeparator);
                dict.Add(Convert.ChangeType(kv[0], argTypes[0]), Convert.ChangeType(kv[1], argTypes[1]));
            }
            field.SetValue(obj, dict);
        }
        else
        { // 일반 타입 및 Enum
            field.SetValue(obj, type.IsEnum ? Enum.Parse(type, value) : Convert.ChangeType(value, type));
        }
    }
    #endregion
}
