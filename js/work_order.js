let contactPointer = null;

async function filterContacts(executionContext) {
  const formContext = executionContext.getFormContext();
  const customerLookup = formContext
    .getAttribute("cr8c9_fk_customer")
    .getValue();

  if (customerLookup && customerLookup.length > 0) {
    const customerId = customerLookup[0].id.replace(/[{}]/g, "");

    const fetchXml = `
      <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
        <entity name="cr8c9_contact">
          <attribute name="cr8c9_contactid" />
          <attribute name="cr8c9_name" />
          <order attribute="cr8c9_name" descending="false" />
          <link-entity name="cr8c9_position" from="cr8c9_fk_contact" to="cr8c9_contactid" link-type="inner" alias="ae">
            <link-entity name="cr8c9_account" from="cr8c9_accountid" to="cr8c9_fk_account" link-type="inner" alias="af">
              <filter type="and">
                <condition attribute="cr8c9_accountid" operator="eq" value="${customerId}" />
              </filter>
            </link-entity>
          </link-entity>
        </entity>
      </fetch>
    `;

    const encodedFetchXml = encodeURIComponent(fetchXml);

    const result = await Xrm.WebApi.retrieveMultipleRecords(
      "cr8c9_contact",
      `?fetchXml=${encodedFetchXml}`
    );

    if (contactPointer !== null) {
      formContext
        .getControl("cr8c9_fk_contact")
        .removePreSearch(contactPointer);
    }

    if (result.entities.length > 0) {
      const contactIds = result.entities.map(
        (contact) => contact.cr8c9_contactid
      );

      contactPointer = function () {
        addCustomFilter(formContext, contactIds);
      };

      formContext.getControl("cr8c9_fk_contact").addPreSearch(contactPointer);
    } else {
      contactPointer = function () {
        applyEmptyFilter(formContext);
      };

      formContext.getControl("cr8c9_fk_contact").addPreSearch(contactPointer);
    }
  }
}

function addCustomFilter(formContext, contactIds) {
  const filterXml = `
    <filter type="and">
      <condition attribute="cr8c9_contactid" operator="in">
        ${contactIds.map((id) => `<value>${id}</value>`).join("")}
      </condition>
    </filter>
  `;

  formContext
    .getControl("cr8c9_fk_contact")
    .addCustomFilter(filterXml, "cr8c9_contact");
}

function applyEmptyFilter(formContext) {
  const emptyFilterXml = `
    <filter type="and">
      <condition attribute="cr8c9_contactid" operator="null" />
    </filter>
  `;

  formContext
    .getControl("cr8c9_fk_contact")
    .addCustomFilter(emptyFilterXml, "cr8c9_contact");
}
