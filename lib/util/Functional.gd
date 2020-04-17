extends Node

static func string_flat(string_array: Array)->String:
  var _new_string = ""

  for _string_item in string_array:
    _new_string += _string_item

  return _new_string

static func filter(filter_function: FuncRef, candidate_array: Array)->Array:
    var filtered_array := []

    for candidate_value in candidate_array:
        if filter_function.call_func(candidate_value):
            filtered_array.append(candidate_value)

    return filtered_array

static func map(function: FuncRef, i_array: Array)->Array:
    var o_array := []
    for value in i_array:
        o_array.append(function.call_func(value))
    return o_array
