function openPopup(formContext) {
  const inventoryId = formContext.data.entity.getId();
  const data = JSON.stringify({
    inventoryId: inventoryId
  });

  let pageInput = {
    pageType: "webresource",
    webresourceName: "cr8c9_inventory_popup_html",
    data: data
  };

  let navigationOptions = {
    target: 2,
    width: 400,
    height: 300,
    position: 1
  };

  Xrm.Navigation.navigateTo(pageInput, navigationOptions).then(
    function success() {},
    function error(e) {
      console.error("Error: ", e.message);
    }
  );
}
