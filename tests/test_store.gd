extends GutTest


func test_store_state_starts_with_debug():
	assert_eq(Store.state.debug, true)
