using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// [자동화 파서 시스템]
/// 1. Reflection을 활용하여 CSV 텍스트를 C# 객체로 자동 변환합니다.
/// 2. 지원 타입: 기본 타입(int, float, string), Enum, 배열(; 구분), 딕셔너리(Key:Value).
/// 3. 제네릭 방식을 사용하여 어떤 데이터 클래스 타입이든 유연하게 처리합니다.
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
        string[] lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1) return list;

        // 1. 헤더 분석 (첫 줄에서 변수 이름 추출)
        string[] headers = lines[0].Split(',');

        // 2. 데이터 줄부터 반복
        // (첫줄은 변수명, 두번 째 줄은 데이터 설명)
        for (int i = 2; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');
            T obj = new T();
            for (int j = 0; j < headers.Length; j++)
            {
                FieldInfo field = typeof(T).GetField(headers[j].Trim());
                if (field != null && j < columns.Length)
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
                newArray.SetValue(Convert.ChangeType(elements[i], elemType), i);
            field.SetValue(obj, newArray);
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
