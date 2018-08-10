﻿using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using OdataCoreTemplate.Classes;
using OdataCoreTemplate.Data;
using OdataCoreTemplate.Models;
using ODataCoreTemplate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("odata/addresses")]
[ODataController(typeof(Address))]

/* Current Issues:
*      Routes will not use OData native format of /users(id), rather than users/id
*    
*  Example on how to get an string[] of roles from the user's token 
*      var roles = User.Claims.Where(c => c.Type == ClaimsIdentity.DefaultRoleClaimType).FirstOrDefault().Value.Split(',');
*/

public class AddressesController : Controller {
    private ApiContext _db;

    public AddressesController(ApiContext context) {
        _db = context;
        // Populate the database if it is empty
        if (context.Addresses.Count() == 0) {
            foreach (var b in MockData.GetAddresses()) {
                context.Addresses.Add(b);
            }
            context.SaveChanges();
        }
    }

    /// <summary>Query addresses</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Address>), 200)] // Ok
    [ProducesResponseType(typeof(void), 404)]  // Not Found
    [EnableQuery]
    public async Task<IActionResult> Get() {
        var addresses = _db.Addresses;
        if (!await addresses.AnyAsync()) {
            return NotFound();
        }
        return Ok(addresses);
    }

    /// <summary>Query addresses by id</summary>
    /// <param name="id">The address id</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(User), 200)] // Ok
    [ProducesResponseType(typeof(void), 404)] // Not Found
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.Select | AllowedQueryOptions.Expand)]
    public async Task<IActionResult> GetSingle(int id) {
        Address address = await _db.Addresses.FindAsync(id);
        if (address == null) {
            return NotFound();
        }
        return Ok(address);
    }

    /// <summary>Create a new address</summary>
    /// <remarks>
    /// Make sure to secure this action before production release
    /// </remarks>
    /// <param name="address">A full address object</param>
    [HttpPost]
    [ProducesResponseType(typeof(User), 201)] // Created
    [ProducesResponseType(typeof(ModelStateDictionary), 400)] // Bad Request
    [ProducesResponseType(typeof(void), 401)] // Unauthorized
    //[Authorize]
    public async Task<IActionResult> Post([FromBody] Address address) {
        if (!ModelState.IsValid) {
            return BadRequest(ModelState);
        }
        _db.Addresses.Add(address);
        await _db.SaveChangesAsync();
        return Created("", address);
    }

    /// <summary>Edit the address with the given id</summary>
    /// <remarks>
    /// Make sure to secure this action before production release
    /// </remarks>
    /// <param name="id">The address id</param>
    /// <param name="addressDelta">A partial address object.  Only properties supplied will be updated.</param>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(User), 200)] // Ok
    [ProducesResponseType(typeof(ModelStateDictionary), 400)] // Bad Request
    [ProducesResponseType(typeof(void), 401)] // Unauthorized
    [ProducesResponseType(typeof(void), 404)] // Not Found
    //[Authorize]
    public async Task<IActionResult> Patch(int id, [FromBody] Delta<Address> addressDelta) {
        if (!ModelState.IsValid) {
            return BadRequest(ModelState);
        }
        var dbAddress = _db.Addresses.Find(id);
        if (dbAddress == null) {
            return NotFound();
        }
        addressDelta.Patch(dbAddress);
        await _db.SaveChangesAsync();
        return Ok(dbAddress);
    }

    /// <summary>Replace all data for the address with the given id</summary>
    /// <remarks>
    /// Make sure to secure this action before production release
    /// </remarks>
    /// <param name="id">The address id</param>
    /// <param name="user">A full address object.  Every property will be updated except id.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(User), 200)] // Ok
    [ProducesResponseType(typeof(ModelStateDictionary), 400)] // Bad Request
    [ProducesResponseType(typeof(void), 401)] // Unauthorized
    [ProducesResponseType(typeof(void), 404)] // Not Found
    //[Authorize]
    public async Task<IActionResult> Put(int id, [FromBody] Delta<Address> address) {
        if (!ModelState.IsValid) {
            return BadRequest(ModelState);
        }
        Address dbAddress = await _db.Addresses.FindAsync(id);
        if (dbAddress == null) {
            return NotFound();
        }
        address.Put(dbAddress);
        await _db.SaveChangesAsync();
        return Ok(dbAddress);
    }

    /// <summary>Delete the given address</summary>
    /// <remarks>
    /// Make sure to secure this action before production release
    /// </remarks>
    /// <param name="id">The address id</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(void), 204)] // No Content
    [ProducesResponseType(typeof(void), 401)] // Unauthorized
    [ProducesResponseType(typeof(void), 404)] // Not Found
    //[Authorize]
    public async Task<IActionResult> Delete(int id) {
        Address address = await _db.Addresses.FindAsync(id);
        if (address == null) {
            return NotFound();
        }
        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();
        return NoContent();
    }


}
