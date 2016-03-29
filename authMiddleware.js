var authCache = {};

/**
 * Middleware for adding the email address and security groups to the
 * request.azureMobile.user object.
 * @param {express.Request} request the Express request
 * @param {express.Response} response the Express response
 * @param {function} next the next piece of middleware
 * @returns {any} the result of the next middleware
 */
    function authMiddleware(request, response, next) {
        if (typeof request.azureMobile.user === 'undefined')
            return next();
        if (typeof request.azureMobile.user.id === 'undefined')
            return next();

        var user = request.azureMobile.user;
        if (typeof authCache[user.id] === 'undefined') {
            user.getIdentity().then(function (userInfo) {

                console.log('-----------------------------------------');
                console.log('USERINFO = ', JSON.stringify(userInfo));
                console.log('-----------------------------------------');

                var groups = userInfo.user_claims.reduce(function (target, claim) {
                    console.log('REDUCE: claim = ', JSON.stringify(claim));
                    console.log('REDUCE: target = ', JSON.stringify(target));
                    if (claim.typ === 'groups') target.push(claim.val);
                    console.log('REDUCE: newtarget = ', JSON.stringify(target));
                    return target;
                }, []);

                console.log('-----------------------------------------');
                console.log('GROUPS = ', JSON.stringify(groups));
                console.log('-----------------------------------------');

                authCache[user.id] = {
                    emailaddress: userInfo.claims.aad.emailaddress,
                    groups: groups
                };

                user.emailaddress = authCache[user.id].emailaddress;
                return next();
            });
        } else {
            user.emailaddress = authCache[user.id].emailaddress;
            return next();
        }
    }

module.exports = authMiddleware;
