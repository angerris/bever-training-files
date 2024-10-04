//------------initialize price list start------------
async function pricelistInit(formContext) {
  const priceListId = formContext.data.entity.getId().replace(/[{}]/g, "");
  const currency = formContext
    .getAttribute("transactioncurrencyid")
    .getValue()[0]
    .id.replace(/[{}]/g, "");

  const [priceListPlural, productPlural, currencyPlural] = await Promise.all([
    getPlural("cr8c9_price_list"),
    getPlural("cr8c9_product"),
    getPlural("transactioncurrency")
  ]);

  await deleteExistingPriceListItems(priceListId);
  await createNewPriceListItems(
    productPlural,
    priceListId,
    currency,
    currencyPlural,
    priceListPlural
  );

  formContext.getControl("price_list_item").refresh();
}

const getPlural = async (entity) => {
  const metadata = await Xrm.Utility.getEntityMetadata(entity, "");
  return metadata.EntitySetName;
};

const deleteExistingPriceListItems = async (priceListId) => {
  const fetchXML = `
    <fetch>
      <entity name="cr8c9_price_list_item">
        <attribute name="cr8c9_price_list_itemid" />
        <filter>
          <condition attribute="cr8c9_fk_price_list" operator="eq" value="${priceListId}" />
        </filter>
      </entity>
    </fetch>`;

  const priceListItems = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_price_list_item",
    `?fetchXml=${encodeURIComponent(fetchXML)}`
  );

  for (const item of priceListItems.entities) {
    await Xrm.WebApi.deleteRecord(
      "cr8c9_price_list_item",
      item.cr8c9_price_list_itemid
    );
  }
};

const createNewPriceListItems = async (
  productPlural,
  priceListId,
  currency,
  currencyPlural,
  priceListPlural
) => {
  const products = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_product",
    "?$select=cr8c9_productid,cr8c9_name"
  );

  for (const product of products.entities) {
    const newPriceListItem = {
      "cr8c9_fk_price_list@odata.bind": `/${priceListPlural}(${priceListId})`,
      "cr8c9_fk_product@odata.bind": `/${productPlural}(${product.cr8c9_productid})`,
      "transactioncurrencyid@odata.bind": `/${currencyPlural}(${currency})`,
      cr8c9_mon_price: 1,
      cr8c9_name: product.cr8c9_name
    };
    await Xrm.WebApi.createRecord("cr8c9_price_list_item", newPriceListItem);
  }
};
//------------initialize price list end------------
